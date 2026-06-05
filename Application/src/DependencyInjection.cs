namespace BuberDinner.Application;

using Microsoft.Extensions.DependencyInjection;
using Trellis.Mediator;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediator(options => options.ServiceLifetime = ServiceLifetime.Scoped);
        // Trellis.Mediator pipeline: Exception/Tracing/Logging/Authorization/Validation behaviors
        // (in canonical order). Authorization/Validation are no-ops when no IActorProvider or
        // IMessageValidator<> is registered, so adoption is free for BuberDinner today and
        // gives Exception (Error.Unexpected with FaultId), Tracing (OTel spans), and Logging
        // (track-aware levels) for every handler.
        services.AddTrellisBehaviors();
        return services;
    }
}
