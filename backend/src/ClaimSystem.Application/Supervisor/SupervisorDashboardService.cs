using ClaimSystem.Application.Claims;
using ClaimSystem.Application.Users;
using ClaimSystem.Domain.Claims;
using ClaimSystem.Domain.Users;

namespace ClaimSystem.Application.Supervisor;

/// <summary>
/// The supervisor's view of the team: where claims are stuck, what nobody has picked up, and who is working on what.
/// </summary>
public sealed record SupervisorDashboardResponse(
    DateTime GeneratedAt,
    IReadOnlyDictionary<ClaimStatus, int> ClaimsByStatus,
    int UnassignedCount,
    IReadOnlyList<DashboardClaim> OldestUnassigned,
    IReadOnlyList<DashboardClaim> LongestInCurrentStatus,
    IReadOnlyList<StaffWorkload> Staff);

/// <summary>An open claim. <see cref="InStatusSince"/> is when it entered its current status.</summary>
public sealed record DashboardClaim(
    Guid Id,
    string ClaimNumber,
    ClaimStatus Status,
    Guid? AssignedStaffId,
    string? AssignedStaffName,
    DateTime SubmittedAt,
    DateTime InStatusSince);

/// <summary>One staff member's open claims (oldest in their current status first).</summary>
public sealed record StaffWorkload(
    Guid StaffId,
    string FullName,
    UserRole Role,
    int OpenClaims,
    IReadOnlyDictionary<ClaimStatus, int> OpenClaimsByStatus,
    IReadOnlyList<DashboardClaim> Claims);

public sealed class SupervisorDashboardService(IClaimRepository claims, IUserRepository users, TimeProvider clock)
{
    /// <summary>Length of the "oldest unassigned" and "longest in current status" lists.</summary>
    public const int ListSize = 10;

    public async Task<SupervisorDashboardResponse> GetAsync(CancellationToken cancellationToken = default)
    {
        var counts = await claims.CountByStatusAsync(cancellationToken);
        var open = await claims.ListOpenAsync(cancellationToken);
        var staff = await users.ListStaffAsync(cancellationToken);

        var staffNames = staff.ToDictionary(s => s.Id, s => s.FullName);
        DashboardClaim ToItem(OpenClaimSnapshot c) => new(
            c.Id, c.ClaimNumber, c.Status, c.AssignedStaffId,
            c.AssignedStaffId is { } id ? staffNames.GetValueOrDefault(id) : null,
            c.SubmittedAt, c.InStatusSince);

        var unassigned = open.Where(c => c.Status == ClaimStatus.Submitted).ToList();

        var workloads = staff
            .Select(member =>
            {
                var mine = open.Where(c => c.AssignedStaffId == member.Id).OrderBy(c => c.InStatusSince).ToList();
                return new StaffWorkload(
                    member.Id, member.FullName, member.Role, mine.Count,
                    mine.GroupBy(c => c.Status).ToDictionary(g => g.Key, g => g.Count()),
                    [.. mine.Select(ToItem)]);
            })
            .OrderByDescending(w => w.OpenClaims)
            .ThenBy(w => w.FullName)
            .ToList();

        return new SupervisorDashboardResponse(
            GeneratedAt: clock.GetUtcNow().UtcDateTime,
            ClaimsByStatus: Enum.GetValues<ClaimStatus>().ToDictionary(s => s, s => counts.GetValueOrDefault(s)),
            UnassignedCount: unassigned.Count,
            OldestUnassigned: [.. unassigned.OrderBy(c => c.SubmittedAt).Take(ListSize).Select(ToItem)],
            LongestInCurrentStatus: [.. open.OrderBy(c => c.InStatusSince).Take(ListSize).Select(ToItem)],
            Staff: workloads);
    }
}
