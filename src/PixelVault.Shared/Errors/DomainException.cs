namespace PixelVault.Shared.Errors;

/// <summary>
/// Base class for all business rule violations thrown from the service layer.
/// The <see cref="DomainExceptionHandler"/> turns these into RFC 7807 problem details responses.
/// </summary>
public abstract class DomainException : Exception
{
    protected DomainException(string code, string message) : base(message)
    {
        Code = code;
    }

    /// <summary>Machine readable error code, e.g. <c>OUT_OF_STOCK</c>.</summary>
    public string Code { get; }

    /// <summary>HTTP status code this exception maps to.</summary>
    public abstract int StatusCode { get; }

    /// <summary>Extra data added to the problem details response.</summary>
    public IDictionary<string, object?> Details { get; } = new Dictionary<string, object?>();
}

/// <summary>400 - the request is invalid. Carries per-field errors.</summary>
public class ValidationException : DomainException
{
    public ValidationException(IDictionary<string, string[]> errors)
        : base("VALIDATION_FAILED", "One or more validation errors occurred.")
    {
        Errors = errors;
    }

    public ValidationException(string field, string error)
        : this(new Dictionary<string, string[]> { [field] = [error] })
    {
    }

    public IDictionary<string, string[]> Errors { get; }

    public override int StatusCode => 400;
}

/// <summary>404 - the requested resource does not exist.</summary>
public class NotFoundException : DomainException
{
    public NotFoundException(string resource, object key)
        : base($"{resource.ToUpperInvariant()}_NOT_FOUND", $"{resource} '{key}' was not found.")
    {
    }

    public override int StatusCode => 404;
}

/// <summary>409 - the request conflicts with the current state of a resource.</summary>
public class ConflictException : DomainException
{
    public ConflictException(string code, string message) : base(code, message)
    {
    }

    public override int StatusCode => 409;
}

/// <summary>422 - the request is well formed but breaks a business rule.</summary>
public class BusinessRuleException : DomainException
{
    public BusinessRuleException(string code, string message) : base(code, message)
    {
    }

    public override int StatusCode => 422;
}

/// <summary>Generic domain exception with an explicit status code.</summary>
public class DomainStatusException : DomainException
{
    private readonly int _statusCode;

    public DomainStatusException(int statusCode, string code, string message) : base(code, message)
    {
        _statusCode = statusCode;
    }

    public override int StatusCode => _statusCode;
}
