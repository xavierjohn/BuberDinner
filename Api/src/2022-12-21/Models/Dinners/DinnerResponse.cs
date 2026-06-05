namespace BuberDinner.Api._2022_12_21.Models.Dinners;

using System;

/// <summary>
/// Wire representation of a <see cref="Domain.Dinner.Entities.Dinner"/>. The same shape
/// is returned by GET, POST (schedule), and the state-transition POSTs (/start, /end, /cancel).
/// </summary>
public record DinnerResponse(
    string Id,
    string Name,
    string Description,
    string HostId,
    string MenuId,
    string Status,
    DateTimeOffset StartDateTime,
    DateTimeOffset EndDateTime,
    DateTimeOffset? StartedAt,
    DateTimeOffset? EndedAt,
    DateTimeOffset? CancelledAt,
    string? CancellationReason);
