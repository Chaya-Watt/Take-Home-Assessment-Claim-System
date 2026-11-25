using System.Text.Json.Serialization;
using ClaimSystem.Api.Auth;
using ClaimSystem.Api.Claims;
using ClaimSystem.Api.Errors;
using ClaimSystem.Api.Policies;
using ClaimSystem.Application;
using ClaimSystem.Infrastructure;
using ClaimSystem.Infrastructure.Persistence;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddAuthorizationPolicies();

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails(); // error responses as RFC 9457 ProblemDetails
builder.Services.AddExceptionHandler<DomainExceptionHandler>(); // DomainException -> 400/403/404/409
builder.Services.AddExceptionHandler<ConcurrencyExceptionHandler>(); // simultaneous saves -> 409
builder.Services.AddHealthChecks();
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter())); // "Claimant", not 0

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy
    .WithOrigins(allowedOrigins)
    .AllowAnyHeader()
    .AllowAnyMethod()));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    await app.Services.InitializeDatabaseAsync(); // apply migrations + seed demo data
}

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();            // /openapi/v1.json
    app.MapScalarApiReference(); // /scalar
}

app.MapHealthChecks("/health");
app.MapAuthEndpoints();
app.MapPolicyEndpoints();
app.MapClaimantClaimEndpoints();
app.MapStaffClaimEndpoints();

app.Run();
