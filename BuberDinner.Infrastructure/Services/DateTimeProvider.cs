namespace BuberDinner.Infrastructure.Services;

using BuberDinner.Application.Common.Interfaces.Services;
internal class DateTimeProvider : IDateTimeProvider
{
    public DateTimeOffset Now => DateTimeOffset.Now;
}
