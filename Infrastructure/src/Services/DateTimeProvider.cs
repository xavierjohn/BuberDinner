namespace BuberDinner.Infrastructure.Services;

using BuberDinner.Application.Abstractions;

internal class DateTimeProvider : IDateTimeProvider
{
    public DateTimeOffset Now => DateTimeOffset.Now;
}
