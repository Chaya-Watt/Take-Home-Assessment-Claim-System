using System.Security.Claims;
using ClaimSystem.Api.Auth;
using ClaimSystem.Application.Auth;
using ClaimSystem.Domain.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace ClaimSystem.IntegrationTests.Auth;

/// <summary>Checks the real policies registered by the API against every role.</summary>
[Collection(ApiCollection.Name)]
public class AuthorizationPolicyTests(WebApplicationFactory<Program> factory)
{
    [Theory]
    [InlineData(AuthorizationPolicies.ClaimantOnly, UserRole.Claimant, true)]
    [InlineData(AuthorizationPolicies.ClaimantOnly, UserRole.Staff, false)]
    [InlineData(AuthorizationPolicies.ClaimantOnly, UserRole.Supervisor, false)]
    [InlineData(AuthorizationPolicies.StaffOrSupervisor, UserRole.Claimant, false)]
    [InlineData(AuthorizationPolicies.StaffOrSupervisor, UserRole.Staff, true)]
    [InlineData(AuthorizationPolicies.StaffOrSupervisor, UserRole.Supervisor, true)]
    [InlineData(AuthorizationPolicies.SupervisorOnly, UserRole.Claimant, false)]
    [InlineData(AuthorizationPolicies.SupervisorOnly, UserRole.Staff, false)]
    [InlineData(AuthorizationPolicies.SupervisorOnly, UserRole.Supervisor, true)]
    public async Task Policy_AllowsOnlyItsRoles(string policy, UserRole role, bool expected)
    {
        var authorization = factory.Services.GetRequiredService<IAuthorizationService>();

        // Same shape as a principal built from our JWT (see JwtBearer options: RoleClaimType = "role").
        var user = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(AuthClaims.UserId, Guid.NewGuid().ToString()), new Claim(AuthClaims.Role, role.ToString())],
            authenticationType: "Test", nameType: AuthClaims.Name, roleType: AuthClaims.Role));

        var result = await authorization.AuthorizeAsync(user, policy);

        Assert.Equal(expected, result.Succeeded);
    }
}
