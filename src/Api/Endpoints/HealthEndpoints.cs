using System.Text.Json;

using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Api.Endpoints;

public static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHealthChecks(
            "/health",
            new HealthCheckOptions
            {
                Predicate = check => check.Tags.Contains("live"),
                ResponseWriter = WriteHealthResponse
            });

        endpoints.MapHealthChecks(
            "/ready",
            new HealthCheckOptions
            {
                Predicate = check => check.Tags.Contains("ready"),
                ResponseWriter = WriteHealthResponse
            });

        return endpoints;
    }

    private static Task WriteHealthResponse(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";

        var payload = new
        {
            status = report.Status.ToString(),
            totalDurationMs = report.TotalDuration.TotalMilliseconds,
            checks = report.Entries.ToDictionary(
                entry => entry.Key,
                entry => new
                {
                    status = entry.Value.Status.ToString(),
                    durationMs = entry.Value.Duration.TotalMilliseconds,
                    description = entry.Value.Description
                })
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(payload));
    }
}
