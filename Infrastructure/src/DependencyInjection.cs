﻿namespace BuberDinner.Infrastructure;

using System.Text;
using BuberDinner.Application.Abstractions;
using BuberDinner.Application.Abstractions.Authentication;
using BuberDinner.Application.Abstractions.Persistence;
using BuberDinner.Domain.Menu;
using BuberDinner.Domain.User.Entities;
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
        return services;
    }

    private static IServiceCollection AddInMemoryDb(this IServiceCollection services)
    {
        services.AddScoped<IRepository<User>, UserInMemoryRepository>();
        services.AddScoped<IRepository<Menu>, MenuInMemoryRepository>();
        return services;
    }
}
