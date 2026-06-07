namespace BuberDinner.Api;

using System.Reflection;
using Asp.Versioning.ApiExplorer;
using BuberDinner.Application.Dinners.Events;
using BuberDinner.Application.Hosts.Authorization;
using BuberDinner.Application.MenuReviews.Events;
using BuberDinner.Application.MenuReviews.Validators;
using BuberDinner.Application.Menus.Commands;
using BuberDinner.Application.Reservations.Events;
using BuberDinner.Domain.Dinner.Events;
using BuberDinner.Domain.Dinner.ValueObject;
using BuberDinner.Domain.Host.ValueObject;
using BuberDinner.Domain.Menu.ValueObject;
using BuberDinner.Domain.MenuReview.Events;
using BuberDinner.Domain.MenuReview.ValueObject;
using BuberDinner.Domain.Reservation.Events;
using BuberDinner.Domain.Reservation.ValueObject;
using Mapster;
using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;
using Trellis;
using Trellis.Asp.Idempotency;
using Trellis.Asp.Routing;
using Trellis.Mediator;
using Trellis.ServiceDefaults;

internal static class DependencyInjection
{
    public static IServiceCollection AddPresentation(this IServiceCollection services)
    {
        services.AddControllers();

        services.AddTrellis(t => t
            .UseAsp()
            .UseScalarValueValidation()
            .UseProblemDetails()
            .UseMediator()
            .UseClaimsActorProvider(opts => opts.ActorIdClaim = "sub")
            .UseResourceAuthorization(
                typeof(UpdateMenuCommand).Assembly,
                typeof(HostResourceLoader).Assembly)
            .UseFluentValidation(typeof(SubmitMenuReviewCommandValidator).Assembly)
            .UseDomainEvents()
            .UseIdempotency(opts =>
            {
                opts.Ttl = TimeSpan.FromHours(24);
                opts.MaxRequestBodyBytes = 256 * 1024;
            }));
        services.AddInMemoryIdempotencyStore();

        services.AddTrellisRouteConstraint<HostId>(nameof(HostId));
        services.AddTrellisRouteConstraint<MenuId>(nameof(MenuId));
        services.AddTrellisRouteConstraint<DinnerId>(nameof(DinnerId));
        services.AddTrellisRouteConstraint<ReservationId>(nameof(ReservationId));
        services.AddTrellisRouteConstraint<MenuReviewId>(nameof(MenuReviewId));

        services.AddDomainEventHandler<DinnerScheduled, LogDinnerScheduledHandler>();
        services.AddDomainEventHandler<DinnerStarted, LogDinnerStartedHandler>();
        services.AddDomainEventHandler<DinnerEnded, LogDinnerEndedHandler>();
        services.AddDomainEventHandler<DinnerCancelled, LogDinnerCancelledHandler>();
        services.AddDomainEventHandler<ReservationCreated, LogReservationCreatedHandler>();
        services.AddDomainEventHandler<ReservationCancelled, LogReservationCancelledHandler>();
        services.AddDomainEventHandler<MenuReviewSubmitted, LogMenuReviewSubmittedHandler>();
        services.AddDomainEventHandler<MenuReviewUpdated, LogMenuReviewUpdatedHandler>();

        services.AddSingleton(TimeProvider.System);

        services.AddMappings();
        services.AddApiVersioning(options => options.ReportApiVersions = true)
                .AddMvc()
                .AddApiExplorer();

        services.AddVersionedOpenApi();

        return services;
    }

    private static IServiceCollection AddVersionedOpenApi(this IServiceCollection services)
    {
        // Resolve API version descriptions once at startup so we can register
        // one OpenAPI document per discovered version.
        using var bootstrap = services.BuildServiceProvider();
        var provider = bootstrap.GetRequiredService<IApiVersionDescriptionProvider>();

        foreach (var description in provider.ApiVersionDescriptions)
        {
            var versionDescription = description;
            services.AddOpenApi(versionDescription.GroupName, options =>
            {
                options.AddDocumentTransformer(new VersionedInfoDocumentTransformer(versionDescription));
                options.AddDocumentTransformer<BearerSecurityDocumentTransformer>();
            });
        }

        return services;
    }

    private static IServiceCollection AddMappings(this IServiceCollection services)
    {
        var config = TypeAdapterConfig.GlobalSettings;
        config.Scan(Assembly.GetExecutingAssembly());
        services.AddSingleton(config);
        services.AddScoped<IMapper, ServiceMapper>();
        return services;
    }
}
