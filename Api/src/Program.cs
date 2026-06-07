using Asp.Versioning.ApiExplorer;
using BuberDinner.Api;
using BuberDinner.Application;
using BuberDinner.Infrastructure;
using Scalar.AspNetCore;
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
    var apiVersionDescriptions = app.Services
        .GetRequiredService<IApiVersionDescriptionProvider>()
        .ApiVersionDescriptions;

    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        foreach (var description in apiVersionDescriptions)
        {
            options.AddDocument(description.GroupName, description.GroupName.ToUpperInvariant());
        }

        options.AddPreferredSecuritySchemes("Bearer");
    });

    app.UseExceptionHandler("/error");
    app.UseHttpsRedirection();
    app.UseRouting();
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseTrellisIdempotency();
    app.MapControllers().RequireAuthorization();
    app.Run();
}

/// <summary>
/// Main program
/// </summary>
public partial class Program { }
