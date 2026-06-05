using BuberDinner.Api;
using BuberDinner.Application;
using BuberDinner.Infrastructure;
using Trellis.Asp.Idempotency;

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
    app.UseRouting();
    app.UseAuthentication();
    app.UseAuthorization();
    // Idempotency middleware MUST sit AFTER UseAuthentication/UseAuthorization so the default
    // per-actor scope sees the authenticated Actor and partitions the store by it. Mounting
    // before authentication would let every authenticated request fall back to the shared
    // 'anonymous' scope, which can let different users collide on the same Idempotency-Key
    // (trellis-api-asp.md:431).
    app.UseTrellisIdempotency();
    app.MapControllers().RequireAuthorization();
    app.Run();
}

/// <summary>
/// Main program
/// </summary>
public partial class Program { }
