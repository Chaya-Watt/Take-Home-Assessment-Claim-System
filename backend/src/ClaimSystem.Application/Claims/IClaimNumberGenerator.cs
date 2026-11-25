namespace ClaimSystem.Application.Claims;

/// <summary>Issues claim numbers like <c>CLM-2026-000001</c>. The sequence restarts every year.</summary>
public interface IClaimNumberGenerator
{
    Task<string> NextAsync(DateTime now, CancellationToken cancellationToken = default);
}
