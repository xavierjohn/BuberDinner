namespace BuberDinner.Application.Abstractions;

public interface IDateTimeProvider
{
    DateTimeOffset Now { get; }
}
