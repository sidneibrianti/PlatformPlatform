using System.Text.Json;
using Microsoft.ApplicationInsights.AspNetCore.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NJsonSchema;
using NJsonSchema.Generation;
using PlatformPlatform.SharedKernel.ApiCore.Aspire;
using PlatformPlatform.SharedKernel.ApiCore.Endpoints;
using PlatformPlatform.SharedKernel.ApiCore.Filters;
using PlatformPlatform.SharedKernel.ApiCore.Middleware;
using PlatformPlatform.SharedKernel.DomainCore.Identity;
using PlatformPlatform.SharedKernel.InfrastructureCore;

namespace PlatformPlatform.SharedKernel.ApiCore;

public static class ApiCoreConfiguration
{
    private const string LocalhostCorsPolicyName = "LocalhostCorsPolicy";

    private static readonly string LocalhostUrl =
        Environment.GetEnvironmentVariable(WebAppMiddlewareConfiguration.PublicUrlKey)!;

    [UsedImplicitly]
    public static IServiceCollection AddApiCoreServices(
        this IServiceCollection services,
        WebApplicationBuilder builder,
        Assembly assembly
    )
    {
        services
            .AddExceptionHandler<TimeoutExceptionHandler>()
            .AddExceptionHandler<GlobalExceptionHandler>()
            .AddTransient<ModelBindingExceptionHandlerMiddleware>()
            .AddProblemDetails()
            .AddEndpointsApiExplorer();

        var applicationInsightsServiceOptions = new ApplicationInsightsServiceOptions
        {
            EnableRequestTrackingTelemetryModule = false,
            EnableDependencyTrackingTelemetryModule = false,
            RequestCollectionOptions = { TrackExceptions = false }
        };

        services.AddApplicationInsightsTelemetry(applicationInsightsServiceOptions);
        services.AddApplicationInsightsTelemetryProcessor<EndpointTelemetryFilter>();

        services.AddOpenApiDocument((settings, serviceProvider) =>
        {
            settings.DocumentName = "v1";
            settings.Title = "PlatformPlatform API";
            settings.Version = "v1";

            var options = (SystemTextJsonSchemaGeneratorSettings)settings.SchemaSettings;
            var serializerOptions = serviceProvider.GetRequiredService<IOptions<JsonOptions>>().Value.SerializerOptions;
            options.SerializerOptions = new JsonSerializerOptions(serializerOptions);

            // Ensure that enums are serialized as strings and use CamelCase
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
            options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;

            settings.PostProcess = document =>
            {
                // Find all strongly typed IDs
                var stronglyTypedIdNames = assembly.GetTypes()
                    .Where(t => typeof(IStronglyTypedId).IsAssignableFrom(t))
                    .Select(t => t.Name)
                    .ToList();

                // Ensure the Swagger UI to correctly display strongly typed IDs as plain text instead of complex objects
                foreach (var stronglyTypedIdName in stronglyTypedIdNames)
                {
                    var schema = document.Definitions[stronglyTypedIdName];
                    schema.Type = JsonObjectType.String;
                    schema.Properties.Clear();
                }
            };
        });

        // Ensure that enums are serialized as strings
        services.Configure<JsonOptions>(options =>
        {
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
            options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        });

        // Ensure correct client IP addresses are set for requests
        // This is required when running behind a reverse proxy like YARP or Azure Container Apps
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            // Enable support for proxy headers such as X-Forwarded-For and X-Forwarded-Proto
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.KnownNetworks.Clear();
            options.KnownProxies.Clear();
        });

        builder.AddServiceDefaults();

        if (builder.Environment.IsDevelopment())
        {
            builder.Services.AddCors(options => options.AddPolicy(
                LocalhostCorsPolicyName,
                policyBuilder => { policyBuilder.WithOrigins(LocalhostUrl).AllowAnyMethod().AllowAnyHeader(); }
            ));
        }
        else
        {
            // When running inside a Docker container running as non-root we need to use a port higher than 1024
            builder.WebHost.ConfigureKestrel(options =>
            {
                options.ListenAnyIP(8443, _ => { });
                options.AddServerHeader = false;
            });
        }

        return services;
    }

    [UsedImplicitly]
    public static WebApplication AddApiCoreConfiguration<TDbContext>(this WebApplication app)
        where TDbContext : DbContext
    {
        if (app.Environment.IsDevelopment())
        {
            // Enable the developer exception page, which displays detailed information about exceptions that occur
            app.UseDeveloperExceptionPage();
            app.UseCors(LocalhostCorsPolicyName);
        }
        else
        {
            // Configure global exception handling for the production environment
            app.UseExceptionHandler(_ => { });
        }

        // Enable support for proxy headers such as X-Forwarded-For and X-Forwarded-Proto. Should run before  other middleware.
        app.UseForwardedHeaders();

        if (!app.Environment.IsDevelopment())
        {
            // Adds middleware for using HSTS, which adds the Strict-Transport-Security header
            // Defaults to 30 days. See https://aka.ms/aspnetcore-hsts, so be careful during development
            app.UseHsts();

            // Adds middleware for redirecting HTTP Requests to HTTPS
            app.UseHttpsRedirection();
        }

        // Enable Swagger UI
        app.UseOpenApi();
        app.UseSwaggerUi();

        app.UseMiddleware<ModelBindingExceptionHandlerMiddleware>();

        // Map default endpoints such as /health, /alive etc.
        app.MapDefaultEndpoints();

        // Configure track endpoint for Application Insights telemetry for PageViews and BrowserTimings
        app.MapTrackEndpoints();

        // Add test-specific endpoints when running tests, such as /api/throwException
        app.MapTestEndpoints();

        app.Services.ApplyMigrations<TDbContext>();

        return app;
    }
}