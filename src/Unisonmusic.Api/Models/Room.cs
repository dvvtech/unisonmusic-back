using System.Collections.Concurrent;

namespace Unisonmusic.Api.Models;

public sealed class Room
{
    public required string Code { get; init; }
    public ConcurrentDictionary<string, User> Users { get; } = new();
    public string? TrackUrl { get; set; }
    public bool IsPlaybackScheduled { get; set; }
    public DateTimeOffset? ScheduledStartAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; init; } = DateTimeOffset.UtcNow;

    // Per-room lock keeps readiness and scheduling decisions atomic.
    public object SyncRoot { get; } = new();

    public bool HasTrack => !string.IsNullOrWhiteSpace(TrackUrl);
    public bool AllUsersReady => HasTrack && Users.Count > 0 && Users.Values.All(user => user.IsReady);
}
