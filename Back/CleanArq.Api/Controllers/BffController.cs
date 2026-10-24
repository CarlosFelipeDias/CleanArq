using System.Security.Claims;
using CleanArq.Api.Security;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CleanArq.Api.Controllers;

[ApiController]
[Route("bff")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class BffController(IAntiforgery antiforgery, IConfiguration configuration) : ControllerBase
{
    [AllowAnonymous, HttpGet("session")]
    public IActionResult Session()
    {
        Response.Headers.CacheControl = "no-store";
        var token = antiforgery.GetAndStoreTokens(HttpContext);
        return Ok(new { authenticated = User.Identity?.IsAuthenticated == true,
            username = User.Identity?.IsAuthenticated == true ? User.Identity.Name : null,
            roles = User.FindAll(ClaimTypes.Role).Select(claim => claim.Value).Distinct().ToArray(), csrfToken = token.RequestToken });
    }
    [AllowAnonymous, HttpGet("login")]
    public IActionResult Login() => Challenge(new AuthenticationProperties
        { RedirectUri = configuration["Bff:PublicOrigin"]!.TrimEnd('/') + "/" }, KeycloakAuthentication.OidcScheme);
    [Authorize, HttpPost("logout")]
    public IActionResult Logout() => SignOut(new AuthenticationProperties
        { RedirectUri = configuration["Bff:PublicOrigin"]!.TrimEnd('/') + "/" }, KeycloakAuthentication.CookieScheme, KeycloakAuthentication.OidcScheme);
}
