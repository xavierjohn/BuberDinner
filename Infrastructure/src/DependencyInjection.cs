namespace BuberDinner.Infrastructure;

using System.Text;
using BuberDinner.Application.Abstractions;
using BuberDinner.Application.Abstractions.Authentication;
using BuberDinner.Application.Abstractions.Persistence;
using BuberDinner.Domain.Menu;
using BuberDinner.Domain.User.Entities;
using HostEntity = BuberDinner.Domain.Host.Entities.Host;
using DinnerEntity = BuberDinner.Domain.Dinner.Entities.Dinner;
using BuberDinner.Infrastructure.Authentication;
using BuberDinner.Infrastructure.Persistence.Cosmos;
using BuberDinner.Infrastructure.Persistence.Memory;
using BuberDinner.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddAuth(configuration)
            .AddSingleton<IDateTimeProvider, DateTimeProvider>();
        if (configuration.GetValue<string>("Persistence") == "CosmosDb")
            services.AddCosmosDb(configuration);
        else
            services.AddInMemoryDb();

        return services;
    }

    private static IServiceCollection AddAuth(this IServiceCollection services, IConfiguration configuration)
    {
        JwtSettings jwtSettings = new();
        configuration.Bind(nameof(JwtSettings), jwtSettings);
        services.AddSingleton(Options.Create(jwtSettings));
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddAuthentication(defaultScheme: JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
                options.TokenValidationParameters = new()
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidAudience = jwtSettings.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret))
                });
        return services;
    }
    private static IServiceCollection AddCosmosDb(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<CosmosDbClientSettings>(configuration.GetSection(nameof(CosmosDbClientSettings)));
        services.AddSingleton<UserCosmosDbContainerSettings>();
        services.AddSingleton<MenuCosmosDbContainerSettings>();
        var cosmosDbClientSettings = new CosmosDbClientSettings();
        configuration.Bind(nameof(CosmosDbClientSettings), cosmosDbClientSettings);
        services.AddSingleton(CosmosClientFactory.InitializeCosmosClientInstance(cosmosDbClientSettings));
        services.AddScoped<IRepository<User>, UserCosmosDbRepository>();
        services.AddScoped<IRepository<Menu>, MenuCosmosDbRepository>();
        // TODO: replace with HostCosmosDbRepository / MenuCosmosDbRepository (paginated) /
        // DinnerCosmosDbRepository when they ship. Until then, fall back to in-memory so a
        // Cosmos-configured deploy doesn't crash on POST /hosts, PUT .../menus/..., or the
        // paginated list/dinner endpoints. Tracked alongside the broader 5-PR roadmap.
        services.AddScoped<IRepository<HostEntity>, HostInMemoryRepository>();
        services.AddScoped<MenuInMemoryRepository>();
        services.AddScoped<IMenuRepository>(sp => sp.GetRequiredService<MenuInMemoryRepository>());
        services.AddScoped<DinnerInMemoryRepository>();
        services.AddScoped<IRepository<DinnerEntity>>(sp => sp.GetRequiredService<DinnerInMemoryRepository>());
        services.AddScoped<IDinnerRepository>(sp => sp.GetRequiredService<DinnerInMemoryRepository>());
        return services;
    }

    private static IServiceCollection AddInMemoryDb(this IServiceCollection services)
    {
        services.AddScoped<IRepository<User>, UserInMemoryRepository>();
        services.AddScoped<MenuInMemoryRepository>();
        services.AddScoped<IRepository<Menu>>(sp => sp.GetRequiredService<MenuInMemoryRepository>());
        services.AddScoped<IMenuRepository>(sp => sp.GetRequiredService<MenuInMemoryRepository>());
        services.AddScoped<IRepository<HostEntity>, HostInMemoryRepository>();
        services.AddScoped<DinnerInMemoryRepository>();
        services.AddScoped<IRepository<DinnerEntity>>(sp => sp.GetRequiredService<DinnerInMemoryRepository>());
        services.AddScoped<IDinnerRepository>(sp => sp.GetRequiredService<DinnerInMemoryRepository>());
        return services;
    }
}
