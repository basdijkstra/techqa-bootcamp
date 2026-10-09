namespace PixelVault.Shared.Errors;

/// <summary>
/// Small helper to collect field validation errors and throw them as one <see cref="ValidationException"/>.
/// </summary>
public sealed class ValidationErrors
{
    private readonly Dictionary<string, List<string>> _errors = new();

    public bool HasErrors => _errors.Count > 0;

    public void Add(string field, string message)
    {
        if (!_errors.TryGetValue(field, out var list))
        {
            list = [];
            _errors[field] = list;
        }

        list.Add(message);
    }

    public void AddIf(bool condition, string field, string message)
    {
        if (condition)
        {
            Add(field, message);
        }
    }

    public IDictionary<string, string[]> ToDictionary() =>
        _errors.ToDictionary(e => e.Key, e => e.Value.ToArray());

    public void ThrowIfAny()
    {
        if (HasErrors)
        {
            throw new ValidationException(ToDictionary());
        }
    }
}
