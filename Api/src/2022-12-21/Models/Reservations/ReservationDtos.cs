namespace BuberDinner.Api._2022_12_21.Models.Reservations;

using System;
using BuberDinner.Domain.Reservation.Entities;
using Mapster;

/// <summary>Wire representation of a <see cref="Reservation"/>.</summary>
public record ReservationResponse(
    string Id,
    string DinnerId,
    string GuestUserId,
    int GuestCount,
    string Status,
    DateTimeOffset ReservedAt,
    DateTimeOffset? CancelledAt,
    string? CancellationReason);

/// <summary>POST /reservations request body.</summary>
public record CreateReservationRequest(string DinnerId, int GuestCount);

/// <summary>POST /reservations/{id}/cancel request body.</summary>
public record CancelReservationRequest(string Reason);

/// <summary>Mapster registration for Reservation → ReservationResponse.</summary>
public sealed class ReservationMappingConfig : IRegister
{
    /// <summary>Registers the <see cref="Reservation"/> → <see cref="ReservationResponse"/> mapping.</summary>
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Reservation, ReservationResponse>()
            .Map(dest => dest.Id, src => src.Id.Value.ToString())
            .Map(dest => dest.DinnerId, src => src.DinnerId.Value.ToString())
            .Map(dest => dest.GuestUserId, src => src.GuestUserId.Value)
            .Map(dest => dest.Status, src => src.Status.Value);
    }
}
