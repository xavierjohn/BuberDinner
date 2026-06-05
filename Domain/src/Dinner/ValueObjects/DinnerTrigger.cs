namespace BuberDinner.Domain.Dinner.ValueObject;

/// <summary>
/// Triggers for the <see cref="Dinner"/> state machine. Encoded as
/// <see cref="RequiredEnum{TSelf}"/> per Cookbook Recipe 9 so equality is symbolic
/// and Stateless's `TTrigger` generic constraint is satisfied.
/// </summary>
/// <remarks>
/// Internal: callers drive transitions through the aggregate's <c>Start</c>, <c>End</c>,
/// <c>Cancel</c> methods. Exposing triggers outside the assembly would let callers bypass
/// the side-effect placement the cookbook is careful about.
/// </remarks>
internal partial class DinnerTrigger : RequiredEnum<DinnerTrigger>
{
    public static readonly DinnerTrigger Start = new();
    public static readonly DinnerTrigger End = new();
    public static readonly DinnerTrigger Cancel = new();
}
