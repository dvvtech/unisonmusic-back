using System.Collections.Concurrent;
using System.Security.Cryptography;
using Unisonmusic.Api.Models;
using Unisonmusic.Api.Models.Dtos;

namespace Unisonmusic.Api.Services;

public sealed class RoomService : IRoomService
{
    private static readonly TimeSpan StartDelay = TimeSpan.FromSeconds(3);

    private readonly ConcurrentDictionary<string, Room> _rooms = new();
    private readonly ConcurrentDictionary<string, string> _connectionRooms = new();
    private readonly ILogger<RoomService> _logger;

    public RoomService(ILogger<RoomService> logger)
    {
        _logger = logger;
    }

    public Room CreateRoom(string connectionId, string? userAgent)
    {
        var room = new Room { Code = GenerateRoomCode() };
        AddUser(room, connectionId, userAgent);
        _rooms[room.Code] = room;
        _connectionRooms[connectionId] = room.Code;

        _logger.LogInformation("Room {RoomCode} created by {ConnectionId}", room.Code, connectionId);
        return room;
    }

    public Room JoinRoom(string roomCode, string connectionId, string? userAgent)
    {
        var normalizedCode = NormalizeRoomCode(roomCode);

        if (!_rooms.TryGetValue(normalizedCode, out var room))
        {
            throw new InvalidOperationException("Комната не найдена.");
        }

        lock (room.SyncRoot)
        {
            AddUser(room, connectionId, userAgent);
            _connectionRooms[connectionId] = normalizedCode;
        }

        _logger.LogInformation("Connection {ConnectionId} joined room {RoomCode}", connectionId, normalizedCode);
        return room;
    }

    public bool TryLeave(string connectionId, out Room? previousRoom)
    {
        previousRoom = null;

        if (!_connectionRooms.TryRemove(connectionId, out var roomCode))
        {
            return false;
        }

        if (!_rooms.TryGetValue(roomCode, out previousRoom))
        {
            return false;
        }

        lock (previousRoom.SyncRoot)
        {
            previousRoom.Users.TryRemove(connectionId, out _);

            if (previousRoom.Users.IsEmpty)
            {
                _rooms.TryRemove(roomCode, out _);
                _logger.LogInformation("Room {RoomCode} removed because it is empty", roomCode);
            }
        }

        return true;
    }

    public Room GetRoomForConnection(string connectionId)
    {
        if (!_connectionRooms.TryGetValue(connectionId, out var roomCode) ||
            !_rooms.TryGetValue(roomCode, out var room))
        {
            throw new InvalidOperationException("Сначала создайте комнату или войдите в нее.");
        }

        return room;
    }

    public Room SetTrack(string connectionId, string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new InvalidOperationException("Укажите корректный HTTP/HTTPS URL трека.");
        }

        var room = GetRoomForConnection(connectionId);

        lock (room.SyncRoot)
        {
            room.TrackUrl = uri.ToString();
            room.IsPlaybackScheduled = false;
            room.ScheduledStartAtUtc = null;

            foreach (var user in room.Users.Values)
            {
                user.IsReady = false;
            }
        }

        _logger.LogInformation("Track changed in room {RoomCode}", room.Code);
        return room;
    }

    public (Room Room, bool AllReady) SetReady(string connectionId)
    {
        var room = GetRoomForConnection(connectionId);

        lock (room.SyncRoot)
        {
            if (!room.HasTrack)
            {
                throw new InvalidOperationException("Сначала укажите URL трека.");
            }

            if (room.Users.TryGetValue(connectionId, out var user))
            {
                user.IsReady = true;
            }

            return (room, room.AllUsersReady && !room.IsPlaybackScheduled);
        }
    }

    public PlaybackCommand SchedulePlayback(Room room, double positionSeconds)
    {
        lock (room.SyncRoot)
        {
            if (!room.HasTrack)
            {
                throw new InvalidOperationException("Сначала укажите URL трека.");
            }

            var now = DateTimeOffset.UtcNow;
            var normalizedPositionSeconds = Math.Max(0, positionSeconds);

            if (room.IsPlaybackScheduled &&
                room.ScheduledStartAtUtc is { } existingStartAt &&
                existingStartAt > now)
            {
                return new PlaybackCommand(room.Code, room.TrackUrl!, normalizedPositionSeconds, existingStartAt, now, (int)StartDelay.TotalMilliseconds);
            }

            var startAt = now.Add(StartDelay);
            room.IsPlaybackScheduled = true;
            room.ScheduledStartAtUtc = startAt;

            _logger.LogInformation("Playback scheduled in room {RoomCode} at {StartAtUtc}", room.Code, startAt);

            return new PlaybackCommand(room.Code, room.TrackUrl!, normalizedPositionSeconds, startAt, now, (int)StartDelay.TotalMilliseconds);
        }
    }

    public IReadOnlyCollection<RoomSnapshotDto> GetSnapshots()
    {
        return _rooms.Values
            .OrderBy(room => room.CreatedAtUtc)
            .Select(ToSnapshot)
            .ToArray();
    }

    public RoomSnapshotDto GetSnapshot(string roomCode)
    {
        var normalizedCode = NormalizeRoomCode(roomCode);

        if (!_rooms.TryGetValue(normalizedCode, out var room))
        {
            throw new InvalidOperationException("Комната не найдена.");
        }

        return ToSnapshot(room);
    }

    public RoomSnapshotDto ToSnapshot(Room room)
    {
        lock (room.SyncRoot)
        {
            var users = room.Users.Values
                .OrderBy(user => user.JoinedAtUtc)
                .Select(user => new UserDto(
                    user.ConnectionId,
                    user.Name,
                    user.IsReady,
                    user.JoinedAtUtc))
                .ToArray();

            return new RoomSnapshotDto(
                room.Code,
                room.TrackUrl,
                room.AllUsersReady,
                room.IsPlaybackScheduled,
                room.ScheduledStartAtUtc,
                users);
        }
    }

    private string GenerateRoomCode()
    {
        for (var attempt = 0; attempt < 100; attempt++)
        {
            var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");

            if (!_rooms.ContainsKey(code))
            {
                return code;
            }
        }

        throw new InvalidOperationException("Не удалось сгенерировать код комнаты.");
    }

    private static void AddUser(Room room, string connectionId, string? userAgent)
    {
        var suffixLength = Math.Min(4, connectionId.Length);
        var baseName = $"Слушатель {connectionId[^suffixLength..].ToUpperInvariant()}";
        var deviceName = GetDeviceName(userAgent);
        var displayName = string.IsNullOrWhiteSpace(deviceName)
            ? baseName
            : $"{baseName} - {deviceName}";

        room.Users[connectionId] = new User
        {
            ConnectionId = connectionId,
            Name = displayName
        };
    }

    private static string? GetDeviceName(string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
        {
            return null;
        }

        var browser = GetBrowserName(userAgent);
        var platform = GetPlatformName(userAgent);

        if (browser is null && platform is null)
        {
            return null;
        }

        return browser is null
            ? platform
            : platform is null
                ? browser
                : $"{browser} / {platform}";
    }

    private static string? GetBrowserName(string userAgent)
    {
        if (userAgent.Contains("YaBrowser", StringComparison.OrdinalIgnoreCase))
        {
            return "Yandex Browser";
        }

        if (userAgent.Contains("Edg/", StringComparison.OrdinalIgnoreCase))
        {
            return "Edge";
        }

        if (userAgent.Contains("SamsungBrowser", StringComparison.OrdinalIgnoreCase))
        {
            return "Samsung Internet";
        }

        if (userAgent.Contains("OPR/", StringComparison.OrdinalIgnoreCase) ||
            userAgent.Contains("Opera", StringComparison.OrdinalIgnoreCase))
        {
            return "Opera";
        }

        if (userAgent.Contains("CriOS", StringComparison.OrdinalIgnoreCase) ||
            userAgent.Contains("Chrome", StringComparison.OrdinalIgnoreCase))
        {
            return "Chrome";
        }

        if (userAgent.Contains("FxiOS", StringComparison.OrdinalIgnoreCase) ||
            userAgent.Contains("Firefox", StringComparison.OrdinalIgnoreCase))
        {
            return "Firefox";
        }

        if (userAgent.Contains("Safari", StringComparison.OrdinalIgnoreCase))
        {
            return "Safari";
        }

        return null;
    }

    private static string? GetPlatformName(string userAgent)
    {
        if (userAgent.Contains("iPhone", StringComparison.OrdinalIgnoreCase))
        {
            return "iPhone";
        }

        if (userAgent.Contains("iPad", StringComparison.OrdinalIgnoreCase))
        {
            return "iPad";
        }

        if (userAgent.Contains("Android", StringComparison.OrdinalIgnoreCase))
        {
            return "Android";
        }

        if (userAgent.Contains("Windows", StringComparison.OrdinalIgnoreCase))
        {
            return "Windows";
        }

        if (userAgent.Contains("Macintosh", StringComparison.OrdinalIgnoreCase) ||
            userAgent.Contains("Mac OS X", StringComparison.OrdinalIgnoreCase))
        {
            return "macOS";
        }

        if (userAgent.Contains("Linux", StringComparison.OrdinalIgnoreCase))
        {
            return "Linux";
        }

        return null;
    }

    private static string NormalizeRoomCode(string roomCode)
    {
        var normalizedCode = roomCode.Trim();

        if (normalizedCode.Length != 6 || !normalizedCode.All(char.IsDigit))
        {
            throw new InvalidOperationException("Код комнаты должен состоять из 6 цифр.");
        }

        return normalizedCode;
    }
}
