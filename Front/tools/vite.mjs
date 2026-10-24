import { spawn, spawnSync } from 'node:child_process';
import { existsSync } from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
const root = fileURLToPath(new URL('../', import.meta.url));
let cwd = root;
let drive;
// Vite treats # in physical paths as a URL fragment. A temporary Windows
// drive alias preserves the original project location without moving files.
if (process.platform === 'win32' && root.includes('#')) {
  drive = 'ZYXWVUTSRQPONMLKJIHGF'.split('').map(letter => `${letter}:`).find(letter => !existsSync(`${letter}\\`));
  if (!drive) throw new Error('No free drive letter is available for the Vite path alias.');
  const result = spawnSync('subst', [drive, path.dirname(root.replace(/[\\/]$/, ''))], { encoding: 'utf8', windowsHide: true });
  if (result.status !== 0) throw new Error(`Unable to create the Vite path alias: ${result.stderr}`);
  cwd = `${drive}\\${path.basename(root.replace(/[\\/]$/, ''))}`;
}
const cleanup = () => { if (drive) { spawnSync('subst', [drive, '/D'], { windowsHide: true }); drive = undefined; } };
const child = spawn(process.execPath, [path.join(cwd, 'node_modules/vite/bin/vite.js'), ...process.argv.slice(2)], { cwd, stdio: 'inherit', windowsHide: true });
child.on('error', error => { cleanup(); console.error(error.message); process.exitCode = 1; });
child.on('exit', code => { cleanup(); process.exitCode = code ?? 1; });
process.on('exit', cleanup);
for (const signal of ['SIGINT', 'SIGTERM']) process.on(signal, () => child.kill(signal));
