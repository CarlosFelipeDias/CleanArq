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
    app.UseSwaggerUI(options => options.UseRequestInterceptor(@"async (request) => {
        if (!['GET','HEAD','OPTIONS'].includes(request.method.toUpperCase())) {
            const response = await fetch('/bff/session', { credentials: 'same-origin', cache: 'no-store' });
            const session = await response.json(); request.headers['X-CSRF-TOKEN'] = session.csrfToken;
        }
        request.credentials = 'same-origin'; return request;
    }"));
    app.MapGet("/", () => Results.Redirect("/swagger")).AllowAnonymous();
}
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
public partial class Program { }
