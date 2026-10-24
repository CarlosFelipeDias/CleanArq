using CleanArq.Application.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CleanArq.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Roles = "admin")]
public sealed class UsersController(CreateUserHandler handler) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(CreatedUser), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request, cancellationToken);
        if (result.Status == UserCreationStatus.Created) return StatusCode(StatusCodes.Status201Created, result.User);
        var statusCode = result.Status switch
        {
            UserCreationStatus.Invalid => StatusCodes.Status400BadRequest,
            UserCreationStatus.Duplicate => StatusCodes.Status409Conflict,
            UserCreationStatus.Unavailable => StatusCodes.Status503ServiceUnavailable,
            _ => StatusCodes.Status502BadGateway
        };
        var problem = new ProblemDetails { Status = statusCode, Title = "User creation failed", Detail = result.Message };
        problem.Extensions["code"] = result.Status.ToString();
        problem.Extensions["traceId"] = HttpContext.TraceIdentifier;
        if (result.UserId is not null) problem.Extensions["userId"] = result.UserId;
        if (result.AccountState is not null) problem.Extensions["accountState"] = result.AccountState;
        return StatusCode(statusCode, problem);
    }
}
