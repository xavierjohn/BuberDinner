namespace BuberDinner.Infrastructure;

using System.Text;
using BuberDinner.Application.Common.Interfaces.Authentication;
using BuberDinner.Application.Common.Interfaces.Persistence;
using BuberDinner.Application.Common.Interfaces.Services;
using BuberDinner.Infrastructure.Authentication;
using BuberDinner.Infrastructure.Persistence;
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
            services.AddCosmosDb();
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
    private static IServiceCollection AddCosmosDb(this IServiceCollection services)
    {
        services.AddSingleton<UserCosmosDbContainerSettings>();
        services.AddSingleton(CosmosClientFactory.InitializeCosmosClientInstance());
        services.AddScoped<IUserRepository, UserCosmosDbRepository>();
        return services;
    }

    private static IServiceCollection AddInMemoryDb(this IServiceCollection services)
    {
        services.AddScoped<IUserRepository, UserInMemoryRepository>();
        return services;
    }
}
