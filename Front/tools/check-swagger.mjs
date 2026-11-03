// Run against a Development API: node tools/check-swagger.mjs http://localhost:5012
// Executes the actual served bootstrap without calling application write endpoints.
import assert from 'node:assert/strict';
import vm from 'node:vm';

const origin = process.argv[2] || 'http://localhost:5012';
const response = await fetch(new URL('/swagger/index.js', origin));
assert.equal(response.status, 200);
let config;
let oauthInitialized = false;
let sessionRequests = 0;
const bundle = value => {
  config = value;
  return { initOAuth() { oauthInitialized = true; } };
};
bundle.presets = { apis: {} };
const context = {
  URL,
  window: { location: { href: new URL('/swagger/index.html', origin).href } },
  SwaggerUIBundle: bundle,
  SwaggerUIStandalonePreset: {},
  fetch: async (url, options) => {
    sessionRequests++;
    assert.equal(url, '/bff/session');
    assert.equal(options.credentials, 'same-origin');
    assert.equal(options.cache, 'no-store');
    return { ok: true, json: async () => ({ csrfToken: 'regression-test-token' }) };
  },
};
vm.runInNewContext(await response.text(), context, { timeout: 5000 });
context.window.onload();
assert.ok(oauthInitialized, 'Swagger must finish initialization');
for (const method of [undefined, 'GET', 'HEAD', 'OPTIONS']) {
  const request = { method, headers: {} };
  assert.equal(await config.requestInterceptor(request), request);
  assert.equal(request.credentials, 'same-origin');
  assert.equal(request.headers['X-CSRF-TOKEN'], undefined);
}
assert.equal(sessionRequests, 0);
for (const method of ['POST', 'PUT', 'PATCH', 'DELETE', 'post']) {
  const request = { method, headers: {} };
  assert.equal(await config.requestInterceptor(request), request);
  assert.equal(request.credentials, 'same-origin');
  assert.equal(request.headers['X-CSRF-TOKEN'], 'regression-test-token');
}
assert.equal(sessionRequests, 5);
context.fetch = async () => ({ ok: false });
await assert.rejects(config.requestInterceptor({ method: 'POST', headers: {} }), /Unable to load/);
console.log('Swagger bootstrap, default GET, CSRF writes and session failure checks passed.');
