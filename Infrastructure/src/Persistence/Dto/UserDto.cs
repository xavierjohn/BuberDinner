﻿// Ignore Spelling: Dto

namespace BuberDinner.Infrastructure.Persistence.Dto;

using BuberDinner.Domain.User.Entities;
using BuberDinner.Domain.User.ValueObjects;
using FunctionalDDD.Domain;

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
        : User.New(
            UserId.New(userDto.Id).Value,
            FirstName.New(userDto.FirstName).Value,
            LastName.New(userDto.LastName).Value,
            EmailAddress.New(userDto.Email).Value,
            Password.New(userDto.Password).Value).Value;
}
