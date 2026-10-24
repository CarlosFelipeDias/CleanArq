using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace CleanArq.Application.Users;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class CreateUserRequest
{
    [Required, EmailAddress, MaxLength(254)]
    public string Email { get; set; } = string.Empty;
    [Required, StringLength(100, MinimumLength = 1)]
    public string FirstName { get; set; } = string.Empty;
    [Required, StringLength(100, MinimumLength = 1)]
    public string LastName { get; set; } = string.Empty;
    [Required, StringLength(128, MinimumLength = 8)]
    public string TemporaryPassword { get; set; } = string.Empty;
}

public enum UserCreationStatus { Created, Invalid, Duplicate, Unavailable, Incomplete, OutcomeUnknown }
public sealed record CreatedUser(string Id, string Email, string FirstName, string LastName, string Role = "user");
public sealed record UserCreationResult(UserCreationStatus Status, CreatedUser? User = null,
    string? Message = null, string? UserId = null, string? AccountState = null);

public interface IUserProvisioner
{
    Task<UserCreationResult> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken);
}

public sealed class CreateUserHandler(IUserProvisioner provisioner)
{
    public Task<UserCreationResult> HandleAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        var normalized = new CreateUserRequest
        {
            Email = request.Email?.Trim() ?? string.Empty,
            FirstName = request.FirstName?.Trim() ?? string.Empty,
            LastName = request.LastName?.Trim() ?? string.Empty,
            TemporaryPassword = request.TemporaryPassword ?? string.Empty
        };
        var errors = new List<ValidationResult>();
        if (!Validator.TryValidateObject(normalized, new ValidationContext(normalized), errors, true)
            || normalized.Email.Any(char.IsWhiteSpace)
            || string.IsNullOrWhiteSpace(normalized.FirstName) || string.IsNullOrWhiteSpace(normalized.LastName))
            return Task.FromResult(new UserCreationResult(UserCreationStatus.Invalid,
                Message: "Provide a valid email, first name, last name and a temporary password of 8 to 128 characters."));
        // The role is deliberately not part of the request contract.
        return provisioner.CreateAsync(normalized, cancellationToken);
    }
}
