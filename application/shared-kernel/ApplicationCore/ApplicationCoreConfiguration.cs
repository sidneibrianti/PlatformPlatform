using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using PlatformPlatform.SharedKernel.ApplicationCore.Behaviors;
using PlatformPlatform.SharedKernel.ApplicationCore.TelemetryEvents;

namespace PlatformPlatform.SharedKernel.ApplicationCore;

public static class ApplicationCoreConfiguration
{
    public static IServiceCollection AddApplicationCoreServices(this IServiceCollection services, Assembly applicationAssembly)
    {
        // Order is important! First all Pre behaviors run, then the command is handled, then all Post behaviors run.
        // So Validation -> Command -> PublishDomainEvents -> UnitOfWork -> PublishTelemetryEvents.
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationPipelineBehavior<,>)); // Pre
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(PublishTelemetryEventsPipelineBehavior<,>)); // Post
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(UnitOfWorkPipelineBehavior<,>)); // Post
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(PublishDomainEventsPipelineBehavior<,>)); // Post
        services.AddScoped<ITelemetryEventsCollector, TelemetryEventsCollector>();
        services.AddScoped<ConcurrentCommandCounter>();
        
        services.AddMediatR(configuration => configuration.RegisterServicesFromAssemblies(applicationAssembly));
        services.AddNonGenericValidators(applicationAssembly);
        
        return services;
    }
    
    /// <summary>
    ///     Registers all non-generic and non-abstract validators in the specified assembly. This is necessary because
    ///     services.AddValidatorsFromAssembly() includes registration of generic and abstract validators.
    /// </summary>
    private static void AddNonGenericValidators(this IServiceCollection services, Assembly assembly)
    {
        var validators = assembly.GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false, IsGenericTypeDefinition: false })
            .SelectMany(type => type.GetInterfaces(), (type, interfaceType) => new { type, interfaceType })
            .Where(t => t.interfaceType.IsGenericType && t.interfaceType.GetGenericTypeDefinition() == typeof(IValidator<>));
        
        foreach (var validator in validators)
        {
            services.AddTransient(validator.interfaceType, validator.type);
        }
    }
}
