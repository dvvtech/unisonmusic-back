namespace Unisonmusic.Api.Models;

public sealed class User
{
    public required string ConnectionId { get; init; }
    public required string Name { get; init; }
    public bool IsReady { get; set; }
    public DateTimeOffset JoinedAtUtc { get; init; } = DateTimeOffset.UtcNow;
}
