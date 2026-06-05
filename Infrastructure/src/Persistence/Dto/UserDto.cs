// Ignore Spelling: Dto

namespace BuberDinner.Infrastructure.Persistence.Dto;

using BuberDinner.Domain.User.Entities;
using BuberDinner.Domain.User.ValueObjects;

public class UserDto
{
    public string Id { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public static class UserDtoExtensions
{
    public static UserDto ToDto(this User user) =>
        new()
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            Password = user.Password
        };

    public static User? ToUser(this UserDto? userDto) =>
        userDto is null
        ? null
        : User.TryCreate(
            UserId.TryCreate(userDto.Id).GetValueOrThrow(nameof(userDto.Id)),
            FirstName.TryCreate(userDto.FirstName).GetValueOrThrow(nameof(userDto.FirstName)),
            LastName.TryCreate(userDto.LastName).GetValueOrThrow(nameof(userDto.LastName)),
            EmailAddress.TryCreate(userDto.Email).GetValueOrThrow(nameof(userDto.Email)),
            Password.TryCreate(userDto.Password).GetValueOrThrow(nameof(userDto.Password))).GetValueOrThrow(nameof(User));
}
