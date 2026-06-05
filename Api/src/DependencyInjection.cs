namespace BuberDinner.Api;

using System.Reflection;
using BuberDinner.Application.Dinners.Events;
using BuberDinner.Application.Hosts.Authorization;
using BuberDinner.Application.Menus.Commands;
using BuberDinner.Domain.Dinner.Events;
using BuberDinner.Domain.Dinner.ValueObject;
using BuberDinner.Domain.Host.ValueObject;
using BuberDinner.Domain.Menu.ValueObject;
using Mapster;
using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using Trellis.Asp.Authorization;
using Trellis.Asp.Routing;
using Trellis.Mediator;

internal static class DependencyInjection
{
    public static IServiceCollection AddPresentation(this IServiceCollection services)
    {
        services.AddControllers();
        services.AddTrellisAspWithScalarValidation();

        // Scalar value-object route constraints — let MVC bind `{hostId:HostId}` / `{menuId:MenuId}`
        // / `{dinnerId:DinnerId}` straight into typed VOs at the boundary.
        services.AddTrellisRouteConstraint<HostId>(nameof(HostId));
        services.AddTrellisRouteConstraint<MenuId>(nameof(MenuId));
        services.AddTrellisRouteConstraint<DinnerId>(nameof(DinnerId));

        // Resource-based authorization wiring: ClaimsActorProvider reads the JWT `sub` claim
        // and the SharedResourceLoaderById<Host, HostId> in the Application assembly authorizes
        // any command implementing IAuthorizeResource<Host> + IIdentifyResource<Host, HostId>.
        services.AddClaimsActorProvider(opts => opts.ActorIdClaim = "sub");
        services.AddResourceAuthorization(
            typeof(UpdateMenuCommand).Assembly,   // Application — commands + IAuthorizeResource implementations
            typeof(HostResourceLoader).Assembly); // Same assembly today; named for clarity / future ACL split

        // Domain event dispatch (Cookbook Recipe 17): explicit per-handler registration is
        // AOT-safe and surfaces intent at the registration site. Each Dinner state transition
        // raises exactly one event; each event has a single side-effect-only logging handler.
        services.AddDomainEventDispatch();
        services.AddDomainEventHandler<DinnerScheduled, LogDinnerScheduledHandler>();
        services.AddDomainEventHandler<DinnerStarted, LogDinnerStartedHandler>();
        services.AddDomainEventHandler<DinnerEnded, LogDinnerEndedHandler>();
        services.AddDomainEventHandler<DinnerCancelled, LogDinnerCancelledHandler>();

        // .NET 8 testable clock — handlers inject TimeProvider rather than DateTime.UtcNow so
        // tests can pin the clock via FakeTimeProvider (Cookbook Recipe 17 §1319).
        services.AddSingleton(TimeProvider.System);

        services.AddMappings();
        services.AddApiVersioning(
                    options =>
                    {
                        options.ReportApiVersions = true;
                    })
                .AddMvc()
                .AddApiExplorer();

        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();
        services.AddSwaggerGen(
            options =>
            {
                // add a custom operation filter which sets default values
                options.OperationFilter<SwaggerDefaultValues>();

                var fileName = typeof(Program).Assembly.GetName().Name + ".xml";
                var filePath = Path.Combine(AppContext.BaseDirectory, fileName);

                // integrate XML comments
                options.IncludeXmlComments(filePath);

                // Set up to allow specifying auth token when executing requests via swagger.
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme()
                {
                    In = ParameterLocation.Header,
                    Description = "Specify token",
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    BearerFormat = "JWT",
                    Scheme = "bearer"
                });

                options.AddSecurityRequirement(new OpenApiSecurityRequirement()
                {
                    {
                        new OpenApiSecurityScheme()
                        {
                            Reference = new OpenApiReference()
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
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
