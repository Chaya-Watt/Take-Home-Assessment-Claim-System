using ClaimSystem.Application.Claims;
using ClaimSystem.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace ClaimSystem.Infrastructure.Persistence;

internal sealed class ClaimNumberGenerator(ClaimDbContext db) : IClaimNumberGenerator
{
    public async Task<string> NextAsync(DateTime now, CancellationToken cancellationToken = default)
    {
        var year = BusinessCalendar.Today(now).Year; // 31 Dec 18:00 UTC is already the new year in Bangkok

        // A single atomic statement: creates this year's counter or increments it, and returns the new value.
        // Two concurrent requests can never read the same value (unlike SELECT MAX + 1).
        var values = await db.Database.SqlQuery<long>($"""
            INSERT INTO ClaimNumberCounters (Year, LastValue) VALUES ({year}, 1)
            ON CONFLICT (Year) DO UPDATE SET LastValue = LastValue + 1
            RETURNING LastValue AS Value
            """).ToListAsync(cancellationToken);

        return $"CLM-{year}-{values.Single():D6}";
    }
}
