namespace Unisonmusic.Api.Models;

public sealed record PlaybackCommand(
    string RoomCode,
    string TrackUrl,
    DateTimeOffset StartAt,
    DateTimeOffset ServerSentAtUtc,
    int StartDelayMs);

public sealed record PauseCommand(
    string RoomCode,
    double PositionSeconds,
    DateTimeOffset PausedAt,
    DateTimeOffset ServerSentAtUtc);
