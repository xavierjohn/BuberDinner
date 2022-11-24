namespace BuberDinner.Application.Tests;

using BuberDinner.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

public class Startup
{
    public static void ConfigureHost(IHostBuilder hostBuilder) =>
        hostBuilder
        .ConfigureServices((context, services) =>
        {
            services.AddApplication();
            services.AddInfrastructure(context.Configuration);

        })
        .ConfigureAppConfiguration((context, builder) =>
        {

            builder.AddJsonFile("appsettings.json");
        });
}
