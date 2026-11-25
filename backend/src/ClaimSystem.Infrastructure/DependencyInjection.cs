using System.Text;
using ClaimSystem.Application.Auth;
using ClaimSystem.Application.Claims;
using ClaimSystem.Application.Common;
using ClaimSystem.Application.Users;
using ClaimSystem.Infrastructure.Auth;
using ClaimSystem.Infrastructure.Persistence;
using ClaimSystem.Infrastructure.Persistence.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ClaimSystem.Infrastructure;

/// <summary>Registers Infrastructure services (DbContext, JWT, Kafka, ...).</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        services.TryAddSingleton(TimeProvider.System);
        services.AddPersistence(configuration);
        services.AddJwtAuthentication(configuration);

        return services;
    }

    private static void AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ClaimDbContext>((sp, options) =>
        {
            var connectionString = new SqliteConnectionStringBuilder(configuration.GetConnectionString("Default")
                ?? throw new InvalidOperationException("Connection string 'Default' is not configured."));

            // A relative path is resolved against the Api project folder (content root),
            // so `dotnet run` and the integration tests use the same claims.db.
            connectionString.DataSource = Path.Combine(
                sp.GetRequiredService<IHostEnvironment>().ContentRootPath, connectionString.DataSource);

            options.UseSqlite(connectionString.ToString());
        });

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<ClaimDbContext>());
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IClaimNumberGenerator, ClaimNumberGenerator>();
    }

    private static void AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .Validate(o => Encoding.UTF8.GetByteCount(o.SigningKey) >= 32, "Jwt:SigningKey must be at least 32 bytes.")
            .Validate(o => o.Issuer.Length > 0 && o.Audience.Length > 0, "Jwt:Issuer and Jwt:Audience are required.")
            .ValidateOnStart(); // fail at startup, not on the first login

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((bearer, jwtOptions) =>
            {
                var jwt = jwtOptions.Value;
                bearer.MapInboundClaims = false; // keep "sub" / "role" as-is instead of long XML claim URIs
                bearer.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = jwt.Issuer,
                    ValidAudience = jwt.Audience,
                    IssuerSigningKey = jwt.CreateSigningKey(),
                    NameClaimType = AuthClaims.Name,
                    RoleClaimType = AuthClaims.Role, // makes RequireRole(...) work
                };
            });

        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IAccessTokenIssuer, JwtAccessTokenIssuer>();
    }
}
