namespace ClaimSystem.Domain.Common;

/// <summary>
/// A business rule was violated. <see cref="Code"/> is stable so the API can map it to an HTTP status.
/// </summary>
public sealed class DomainException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}

/// <summary>Error codes raised by the domain.</summary>
public static class DomainErrors
{
    /// <summary>Action not allowed in the claim's current status.</summary>
    public const string InvalidTransition = "claim.invalid_transition";

    /// <summary>Input breaks a business rule (amounts, dates, incident type, ...).</summary>
    public const string Validation = "validation";

    /// <summary>The acting user is not the claim's owner / assigned staff.</summary>
    public const string NotAllowed = "not_allowed";

    /// <summary>A referenced child item (e.g. information request) does not exist.</summary>
    public const string NotFound = "not_found";
}
