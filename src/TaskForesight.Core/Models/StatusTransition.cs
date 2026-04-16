namespace TaskForesight.Core.Models;

public record StatusTransition(
    string FromStatus,
    string ToStatus,
    string Author,
    DateTimeOffset TransitionedAt);
