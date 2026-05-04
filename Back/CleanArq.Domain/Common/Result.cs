namespace CleanArq.Domain.Common;

/// <summary>
/// Represents the result of an operation, encapsulating success or failure
/// with associated error notifications
/// </summary>
public abstract record Result
{
    private static readonly Notification NullValueNotification =
        new("Value", "Value cannot be null");

    public IReadOnlyList<Notification> Errors { get; }

    protected Result(IReadOnlyList<Notification> errors)
    {
        Errors = errors;
    }

    public bool IsSuccess => Errors.Count == 0;
    public bool IsFailure => !IsSuccess;

    public static Result Success() => new VoidSuccessResult(new List<Notification>());
    public static Result<TValue> Success<TValue>(TValue value) =>
        value is not null
            ? new Result<TValue>.SuccessResultWithValue(value, new List<Notification>())
            : new Result<TValue>.FailureResultWithValue(new List<Notification> { NullValueNotification });

    public static Result Failure(params Notification[] errors) => new VoidFailureResult(errors.ToList());
    public static Result<TValue> Failure<TValue>(params Notification[] errors) => new Result<TValue>.FailureResultWithValue(errors.ToList());

    public static Result Failure(IEnumerable<Notification> errors) => new VoidFailureResult(errors.ToList());
    public static Result<TValue> Failure<TValue>(IEnumerable<Notification> errors) => new Result<TValue>.FailureResultWithValue(errors.ToList());

    private sealed record VoidSuccessResult(IReadOnlyList<Notification> Errors) : Result(Errors);
    private sealed record VoidFailureResult(IReadOnlyList<Notification> Errors) : Result(Errors);
}

/// <summary>
/// Resultado genérico que encapsula um valor do tipo TValue
/// </summary>
public abstract record Result<TValue> : Result
{
    public TValue? Value { get; }

    protected Result(TValue? value, IReadOnlyList<Notification> errors) : base(errors)
    {
        Value = value;
    }

    public static implicit operator Result<TValue>(TValue? value) =>
        value is not null
            ? new SuccessResultWithValue(value, new List<Notification>())
            : new FailureResultWithValue(new List<Notification> { new("Value", "Value cannot be null") });

    public static implicit operator Result<TValue>(Notification notification) =>
        new FailureResultWithValue(new List<Notification> { notification });

    public static implicit operator Result<TValue>(List<Notification> notifications) =>
        new FailureResultWithValue(notifications);

    internal sealed record SuccessResultWithValue(TValue Value, IReadOnlyList<Notification> Errors) : Result<TValue>(Value, Errors);
    internal sealed record FailureResultWithValue(IReadOnlyList<Notification> Errors) : Result<TValue>(default, Errors);
}
