using Microsoft.AspNetCore.SignalR;
using Unisonmusic.Api.Models;
using Unisonmusic.Api.Models.Dtos;
using Unisonmusic.Api.Services;

namespace Unisonmusic.Api.Hubs;

public sealed class MusicHub : Hub
{
    private readonly IRoomService _roomService;
    private readonly ILogger<MusicHub> _logger;

    public MusicHub(IRoomService roomService, ILogger<MusicHub> logger)
    {
        _roomService = roomService;
        _logger = logger;
    }

    public async Task<RoomSnapshotDto> CreateRoom(ClientDeviceInfo? deviceInfo = null)
    {
        try
        {
            await LeaveCurrentRoomAsync();

            var room = _roomService.CreateRoom(Context.ConnectionId, deviceInfo, GetUserAgent());
            await Groups.AddToGroupAsync(Context.ConnectionId, room.Code);

            var snapshot = _roomService.ToSnapshot(room);
            await Clients.Caller.SendAsync("RoomJoined", snapshot);

            return snapshot;
        }
        catch (InvalidOperationException exception)
        {
            throw new HubException(exception.Message);
        }
    }

    public async Task<RoomSnapshotDto> JoinRoom(string roomCode, ClientDeviceInfo? deviceInfo = null)
    {
        try
        {
            await LeaveCurrentRoomAsync();

            var room = _roomService.JoinRoom(roomCode, Context.ConnectionId, deviceInfo, GetUserAgent());
            await Groups.AddToGroupAsync(Context.ConnectionId, room.Code);

            var snapshot = _roomService.ToSnapshot(room);
            await Clients.Caller.SendAsync("RoomJoined", snapshot);
            await Clients.Group(room.Code).SendAsync("RoomUpdated", snapshot);

            return snapshot;
        }
        catch (InvalidOperationException exception)
        {
            throw new HubException(exception.Message);
        }
    }

    public async Task<RoomSnapshotDto> SetTrack(string url)
    {
        try
        {
            var room = _roomService.SetTrack(Context.ConnectionId, url);
            var snapshot = _roomService.ToSnapshot(room);

            await Clients.Group(room.Code).SendAsync("TrackUpdated", snapshot);
            await Clients.Group(room.Code).SendAsync("RoomUpdated", snapshot);

            return snapshot;
        }
        catch (InvalidOperationException exception)
        {
            throw new HubException(exception.Message);
        }
    }

    public async Task<RoomSnapshotDto> SetReady()
    {
        try
        {
            var (room, allReady) = _roomService.SetReady(Context.ConnectionId);
            var snapshot = _roomService.ToSnapshot(room);

            await Clients.Group(room.Code).SendAsync("UserReadyUpdate", snapshot);

            if (allReady)
            {
                await SendPlaybackCommandAsync(room, 0);
            }

            return snapshot;
        }
        catch (InvalidOperationException exception)
        {
            throw new HubException(exception.Message);
        }
    }

    public async Task<PlaybackCommand> StartPlayback()
    {
        try
        {
            var room = _roomService.GetRoomForConnection(Context.ConnectionId);
            return await SendPlaybackCommandAsync(room, 0);
        }
        catch (InvalidOperationException exception)
        {
            throw new HubException(exception.Message);
        }
    }

    public async Task<PlaybackCommand> ContinuePlayback(double positionSeconds)
    {
        try
        {
            var room = _roomService.GetRoomForConnection(Context.ConnectionId);
            return await SendPlaybackCommandAsync(room, positionSeconds);
        }
        catch (InvalidOperationException exception)
        {
            throw new HubException(exception.Message);
        }
    }

    public async Task<PauseCommand> PausePlayback(double positionSeconds)
    {
        try
        {
            var room = _roomService.GetRoomForConnection(Context.ConnectionId);
            var now = DateTimeOffset.UtcNow;
            var normalizedPositionSeconds = Math.Max(0, positionSeconds);
            var command = new PauseCommand(room.Code, normalizedPositionSeconds, now, now);

            lock (room.SyncRoot)
            {
                room.IsPlaybackScheduled = false;
                room.ScheduledStartAtUtc = null;
            }

            await Clients.Group(room.Code).SendAsync("ReceivePause", command);

            _logger.LogInformation(
                "Sent playback pause for room {RoomCode} at {PositionSeconds}s: {PausedAtUtc}",
                room.Code,
                command.PositionSeconds,
                command.PausedAt);

            return command;
        }
        catch (InvalidOperationException exception)
        {
            throw new HubException(exception.Message);
        }
    }

    public Task<DateTimeOffset> GetServerTime()
    {
        return Task.FromResult(DateTimeOffset.UtcNow);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await LeaveCurrentRoomAsync();
        await base.OnDisconnectedAsync(exception);
    }

    private async Task<PlaybackCommand> SendPlaybackCommandAsync(Room room, double positionSeconds)
    {
        var command = _roomService.SchedulePlayback(room, positionSeconds);
        await Clients.Group(room.Code).SendAsync("ReceiveStartTime", command);

        _logger.LogInformation(
            "Sent playback start for room {RoomCode} at {PositionSeconds}s: {StartAtUtc}",
            room.Code,
            command.PositionSeconds,
            command.StartAt);

        return command;
    }

    private async Task LeaveCurrentRoomAsync()
    {
        if (!_roomService.TryLeave(Context.ConnectionId, out var previousRoom) || previousRoom is null)
        {
            return;
        }

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, previousRoom.Code);

        if (!previousRoom.Users.IsEmpty)
        {
            var snapshot = _roomService.ToSnapshot(previousRoom);
            await Clients.Group(previousRoom.Code).SendAsync("RoomUpdated", snapshot);
            await Clients.Group(previousRoom.Code).SendAsync("UserReadyUpdate", snapshot);
        }
    }

    private string? GetUserAgent()
    {
        return Context.GetHttpContext()?.Request.Headers["User-Agent"].ToString();
    }
}
