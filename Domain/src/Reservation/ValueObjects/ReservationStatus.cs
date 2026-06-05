namespace BuberDinner.Domain.Reservation.ValueObject;

public partial class ReservationStatus : RequiredEnum<ReservationStatus>
{
    public static readonly ReservationStatus Reserved = new();
    public static readonly ReservationStatus Cancelled = new();
}

internal partial class ReservationTrigger : RequiredEnum<ReservationTrigger>
{
    public static readonly ReservationTrigger Cancel = new();
}
