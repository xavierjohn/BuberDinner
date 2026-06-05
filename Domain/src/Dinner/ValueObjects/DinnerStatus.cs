namespace BuberDinner.Domain.Dinner.ValueObject;

/// <summary>
/// Lifecycle state of a <see cref="Dinner"/>. Encoded as <see cref="RequiredEnum{TSelf}"/>
/// so equality is symbolic and the value object satisfies the Stateless `TState` generic
/// constraint per Cookbook Recipe 9.
/// </summary>
public partial class DinnerStatus : RequiredEnum<DinnerStatus>
{
    /// <summary>Scheduled but not yet started. The only state from which <c>Start</c> or <c>Cancel</c> are permitted.</summary>
    public static readonly DinnerStatus Upcoming = new();

    /// <summary>Currently running. The only state from which <c>End</c> is permitted.</summary>
    public static readonly DinnerStatus InProgress = new();

    /// <summary>Ran to completion. Terminal — no further transitions.</summary>
    public static readonly DinnerStatus Ended = new();

    /// <summary>Called off before it began. Terminal — no further transitions.</summary>
    /// <remarks>
    /// `Cancelled` means "never happened". A dinner that started and then was terminated early
    /// transitions to <see cref="Ended"/> instead, so downstream consumers (refunds, no-show
    /// tracking, etc.) can distinguish "the event ran" from "the event was called off".
    /// </remarks>
    public static readonly DinnerStatus Cancelled = new();
}
