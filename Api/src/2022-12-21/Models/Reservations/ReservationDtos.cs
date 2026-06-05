namespace BuberDinner.Api._2022_12_21.Models.Reservations;

using System;
using BuberDinner.Application.Reservations.Commands;
using BuberDinner.Domain.Reservation.Entities;
using BuberDinner.Domain.User.ValueObjects;
using Mapster;
using DinnerIdClass = BuberDinner.Domain.Dinner.ValueObject.DinnerId;

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
public record CreateReservationRequest(string DinnerId, int GuestCount)
{
    /// <summary>
    /// Lifts the request into a validated <see cref="CreateReservationCommand"/> using the
    /// railway pipeline. A bad <see cref="DinnerId"/> surfaces as the framework's standard
    /// 422 Problem Details shape (one FieldViolation per offending field) — same as every
    /// other request DTO in the codebase. Hand-rolling the 422 body in the controller
    /// would diverge from the Trellis problem-details contract.
    /// </summary>
    public Result<CreateReservationCommand> ToCreateReservationCommand(UserId guestUserId) =>
        DinnerIdClass.TryCreate(this.DinnerId, nameof(DinnerId))
            .Map(dinnerId => new CreateReservationCommand(dinnerId, guestUserId, this.GuestCount));
}

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
