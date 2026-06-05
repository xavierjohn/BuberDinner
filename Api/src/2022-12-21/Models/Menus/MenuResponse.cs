namespace BuberDinner.Api._2022_12_21.Models.Menus;

/// <summary>
/// Menu retrieval/response shape — used for GET /menus/{id} and PUT /menus/{id}.
/// Same shape as <see cref="CreateMenuResponse"/>; declared separately so the OpenAPI
/// contract distinguishes the read and write models.
/// </summary>
public record MenuResponse(
    string Id,
    string Name,
    string Description,
    decimal? AverageRating,
    List<MenuSectionResponse> Sections,
    string HostId,
    List<string> DinnerIds,
    List<string> MenuReviewIds);
