namespace BuberDinner.Api._2022_12_21.Models.Menus;

/// <summary>
/// Create menu response model
/// </summary>
/// <param name="Id">The id of the menu</param>
/// <param name="Name">The name of the menu</param>
/// <param name="Description">The description of the menu</param>
/// <param name="AverageRating">The average rating of the menu</param>
/// <param name="Sections">List of menu sections</param>
/// <param name="HostId">The id of the host that owns the menu</param>
/// <param name="DinnerIds">List of ids of the dinners using the menu</param>
/// <param name="MenuReviewIds">List of ids of the reviews of the menu</param>
public record CreateMenuResponse(
    string Id,
    string Name,
    string Description,
    decimal? AverageRating,
    List<MenuSectionResponse> Sections,
    string HostId,
    List<string> DinnerIds,
    List<string> MenuReviewIds);

/// <summary>
/// Menu section response model
/// </summary>
/// <param name="Id">The id of the menu section</param>
/// <param name="Name">The name of the menu section</param>
/// <param name="Description">The description of the menu section</param>
/// <param name="Items">List of menu items</param>
public record MenuSectionResponse(
    string Id,
    string Name,
    string Description,
    List<MenuItemResponse> Items);

/// <summary>
/// Menu item response model
/// </summary>
/// <param name="Id">The id of the menu item</param>
/// <param name="Name">The name of the menu item</param>
/// <param name="Description">The description of the menu item</param>
public record MenuItemResponse(
    string Id,
    string Name,
    string Description);
