using System.Text.Json.Serialization;
using ClaimSystem.Api.Auth;
using ClaimSystem.Api.Claims;
using ClaimSystem.Api.Errors;
using ClaimSystem.Api.Policies;
using ClaimSystem.Api.Supervisor;
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
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()); // "Claimant", not 0
    // Numbers must be JSON numbers (not "123" strings); this also keeps OpenAPI types as plain integers.
    options.SerializerOptions.NumberHandling = JsonNumberHandling.Strict;
});

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

app.UseExceptionHandler(new ExceptionHandlerOptions
{
    // A malformed request body (e.g. a number where text is expected) is the caller's fault: 400, not 500.
    // In Development, Minimal APIs throw BadHttpRequestException for it instead of answering 400 directly.
    StatusCodeSelector = exception => exception is BadHttpRequestException badRequest
        ? badRequest.StatusCode
        : StatusCodes.Status500InternalServerError,
});
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
app.MapSupervisorEndpoints();

app.Run();
