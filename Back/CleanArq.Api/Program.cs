using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi;
using CleanArq.Infra.IoC;
using CleanArq.Api.Security;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Services.AddControllersWithViews(options => options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Session", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.ApiKey, In = ParameterLocation.Cookie, Name = "CleanArq.Session",
        Description = "Sign in through /bff/login. The browser sends the HttpOnly session cookie."
    });
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Session", document)] = new List<string>()
    });
});
builder.Services.AddInfrastructure(builder.Configuration, builder.Environment.IsDevelopment());
builder.Services.AddKeycloakAuthentication(builder.Configuration, builder.Environment);
var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
    app.UseSwagger();
    // Swagger embeds this in a JavaScript string before JSON.parse: keep it on one line.
    // Its function parser also requires a regular function, rather than async/await.
    app.UseSwaggerUI(options => options.UseRequestInterceptor(
        "function (request) { " +
        "request.credentials = 'same-origin'; " +
        "if (!['GET','HEAD','OPTIONS'].includes((request.method || 'GET').toUpperCase())) { " +
        "return fetch('/bff/session', { credentials: 'same-origin', cache: 'no-store' }) " +
        ".then(function (response) { " +
        "if (!response.ok) throw new Error('Unable to load the session antiforgery token.'); " +
        "return response.json(); }) " +
        ".then(function (session) { request.headers['X-CSRF-TOKEN'] = session.csrfToken; return request; }); " +
        "} return request; }"));
    app.MapGet("/", () => Results.Redirect("/swagger")).AllowAnonymous();
}
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
public partial class Program { }
