using BuberDinner.Api;
using BuberDinner.Application;
using BuberDinner.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
{
    builder.Services
        .AddPresentation()
        .AddApplication()
        .AddInfrastructure(builder.Configuration);
}

var app = builder.Build();
{
    app.UseSwagger();
    app.UseSwaggerUI(
        options =>
        {
            options.RoutePrefix = string.Empty;
            var descriptions = app.DescribeApiVersions();

            // build a swagger endpoint for each discovered API version
            foreach (var description in descriptions)
            {
                var url = $"/swagger/{description.GroupName}/swagger.json";
                var name = description.GroupName.ToUpperInvariant();
                options.SwaggerEndpoint(url, name);
            }
        });
    app.UseExceptionHandler("/error");
    app.UseHttpsRedirection();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers().RequireAuthorization();
    app.Run();
}

/// <summary>
/// Main program
/// </summary>
public partial class Program { }
