namespace BuberDinner.Api;

using System.Reflection;
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
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
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

        services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();
        services.AddSwaggerGen(options =>
        {
            options.OperationFilter<SwaggerDefaultValues>();
            var fileName = typeof(Program).Assembly.GetName().Name + ".xml";
            var filePath = Path.Combine(AppContext.BaseDirectory, fileName);
            options.IncludeXmlComments(filePath);
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                In = ParameterLocation.Header,
                Description = "Specify token",
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                BearerFormat = "JWT",
                Scheme = "bearer",
            });
            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer",
                        },
                    },
                    Array.Empty<string>()
                },
            });
        });
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
