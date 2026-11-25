using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ClaimSystem.Infrastructure.Persistence;

/// <summary>Writes DateTimes as UTC and marks values read back as <see cref="DateTimeKind.Utc"/>.</summary>
internal sealed class UtcDateTimeConverter() : ValueConverter<DateTime, DateTime>(
    value => value.ToUniversalTime(),
    value => DateTime.SpecifyKind(value, DateTimeKind.Utc));
