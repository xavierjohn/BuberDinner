﻿namespace BuberDinner.Infrastructure.Persistence;

using BuberDinner.Application.Common.Interfaces.Persistence;
using BuberDinner.Domain.Entities;
using CSharpFunctionalExtensions;
using System.Collections.Generic;
using System.Linq;

internal class UserRepository : IUserRepository
{
    private static readonly List<User> s_users = new();

    public void Add(User user) => s_users.Add(user);

    public Task<Maybe<User>> GetUserByEmail(string email) =>
        Task.FromResult(s_users.SingleOrDefault(u => u.Email == email) ?? Maybe<User>.None);
}