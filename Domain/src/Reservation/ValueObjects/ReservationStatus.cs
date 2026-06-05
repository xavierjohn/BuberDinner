namespace BuberDinner.Domain.Reservation.ValueObject;

/// <summary>
/// Lifecycle state of a <see cref="Reservation"/>. Two states only: a reservation is
/// either active (<see cref="Reserved"/>) or has been called off (<see cref="Cancelled"/>).
/// </summary>
public partial class ReservationStatus : RequiredEnum<ReservationStatus>
{
    /// <summary>The reservation is active. The only state from which <c>Cancel</c> is permitted.</summary>
    public static readonly ReservationStatus Reserved = new();

    /// <summary>Terminal — the guest called the reservation off before the dinner started.</summary>
    public static readonly ReservationStatus Cancelled = new();
}

internal partial class ReservationTrigger : RequiredEnum<ReservationTrigger>
{
    public static readonly ReservationTrigger Cancel = new();
}
