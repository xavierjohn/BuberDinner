namespace BuberDinner.Application.Abstractions.Persistence;

using System.Collections.Generic;
using BuberDinner.Domain.Menu.ValueObject;
using BuberDinner.Domain.MenuReview.Entities;

public interface IMenuReviewRepository : IRepository<MenuReview>
{
    IReadOnlyList<MenuReview> GetPageForMenu(MenuId menuId, Trellis.PageSize pageSize, System.Guid? afterId);
}
