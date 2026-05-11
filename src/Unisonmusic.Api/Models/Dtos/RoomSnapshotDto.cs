namespace Unisonmusic.Api.Models.Dtos;

public sealed record RoomSnapshotDto(
    string RoomCode,
    string? TrackUrl,
    string? TrackSourceUrl,
    string? TrackTitle,
    bool AllReady,
    bool IsPlaybackScheduled,
    DateTimeOffset? ScheduledStartAtUtc,
    IReadOnlyCollection<UserDto> Users);
