namespace BuberDinner.Api;

using System.Text;
using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

/// <summary>
/// Populates <see cref="OpenApiInfo"/> for a single API version.
/// </summary>
internal sealed class VersionedInfoDocumentTransformer(ApiVersionDescription description) : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        document.Info = CreateInfo(description);
        return Task.CompletedTask;
    }

    private static OpenApiInfo CreateInfo(ApiVersionDescription description)
    {
        var text = new StringBuilder("Buber Dinner the AirBnB for dinner.");
        var info = new OpenApiInfo
        {
            Title = "Buber Dinner",
            Version = description.ApiVersion.ToString(),
            Contact = new OpenApiContact { Name = "Xavier John", Email = "xavier@somewhere.com" },
            License = new OpenApiLicense { Name = "MIT", Url = new Uri("https://opensource.org/licenses/MIT") },
        };

        if (description.IsDeprecated)
        {
            text.Append(" This API version has been deprecated.");
        }

        if (description.SunsetPolicy is SunsetPolicy policy)
        {
            if (policy.Date is DateTimeOffset when)
            {
                text.Append(" The API will be sunset on ")
                    .Append(when.Date.ToShortDateString())
                    .Append('.');
            }

            if (policy.HasLinks)
            {
                text.AppendLine();
                for (var i = 0; i < policy.Links.Count; i++)
                {
                    var link = policy.Links[i];
                    if (link.Type == "text/html")
                    {
                        text.AppendLine();
                        if (link.Title.HasValue)
                        {
                            text.Append(link.Title.Value).Append(": ");
                        }
                        text.Append(link.LinkTarget.OriginalString);
                    }
                }
            }
        }

        info.Description = text.ToString();
        return info;
    }
}

/// <summary>
/// Adds the JWT Bearer security scheme and a document-wide security requirement.
/// </summary>
internal sealed class BearerSecurityDocumentTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Specify token",
        };

        document.Security ??= [];
        document.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", document)] = new List<string>(),
        });

        return Task.CompletedTask;
    }
}
