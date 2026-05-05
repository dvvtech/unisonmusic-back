namespace Unisonmusic.Api.Models.Dtos;

public sealed record RoomSnapshotDto(
    string RoomCode,
    string? TrackUrl,
    bool AllReady,
    bool IsPlaybackScheduled,
    DateTimeOffset? ScheduledStartAtUtc,
    IReadOnlyCollection<UserDto> Users);
