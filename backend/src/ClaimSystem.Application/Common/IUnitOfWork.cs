namespace ClaimSystem.Application.Common;

/// <summary>Saves everything changed in the current request in one transaction.</summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
