﻿namespace BuberDinner.Api.Neutral.Models.Authentication;

using BuberDinner.Application.Services.Authentication.Common;
using Mapster;

internal class AuthenticationMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<AuthenticationResult, AuthenticationResponse>()
            .Map(dest => dest.Token, src => src.Token)
            .Map(dest => dest.UserId, src => src.User.Id)
            .Map(dest => dest, src => src.User);
    }
}
