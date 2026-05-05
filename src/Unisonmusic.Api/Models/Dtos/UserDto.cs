namespace Unisonmusic.Api.Models.Dtos;

public sealed record UserDto(
    string Id,
    string Name,
    bool IsReady,
    DateTimeOffset JoinedAtUtc);
