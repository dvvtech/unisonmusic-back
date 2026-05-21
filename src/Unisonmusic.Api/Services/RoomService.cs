using System.Collections.Concurrent;
using System.Security.Cryptography;
using Unisonmusic.Api.Models;
using Unisonmusic.Api.Models.Dtos;
using Unisonmusic.Api.Services.Abstract;

namespace Unisonmusic.Api.Services;

public sealed class RoomService : IRoomService
{
    private static readonly TimeSpan StartDelay = TimeSpan.FromMilliseconds(500);
    private const int ShortRoomCodeLength = 3;
    private const int LongRoomCodeLength = 4;
    private const int RoomCodeExpansionThreshold = 900;
    private const int MaxRoomCount = 9999;

    private readonly ConcurrentDictionary<string, Room> _rooms = new();
    private readonly ConcurrentDictionary<string, string> _connectionRooms = new();
    private readonly object _roomCreationSync = new();
    private readonly ILogger<RoomService> _logger;

    public RoomService(ILogger<RoomService> logger)
    {
        _logger = logger;
    }

    public Room CreateRoom(string connectionId, ClientDeviceInfo? deviceInfo, string? userAgent)
    {
        Room room;

        lock (_roomCreationSync)
        {
            if (_rooms.Count >= MaxRoomCount)
            {
                throw new InvalidOperationException($"Достигнуто максимальное количество комнат: {MaxRoomCount}.");
            }

            room = new Room { Code = GenerateRoomCode() };
            AddUser(room, connectionId, deviceInfo, userAgent);
            _rooms[room.Code] = room;
            _connectionRooms[connectionId] = room.Code;
        }

        _logger.LogInformation("Room {RoomCode} created by {ConnectionId}", room.Code, connectionId);
        return room;
    }

    public Room JoinRoom(string roomCode, string connectionId, ClientDeviceInfo? deviceInfo, string? userAgent)
    {
        var normalizedCode = NormalizeRoomCode(roomCode);

        if (!_rooms.TryGetValue(normalizedCode, out var room))
        {
            throw new InvalidOperationException("Комната не найдена.");
        }

        lock (room.SyncRoot)
        {
            AddUser(room, connectionId, deviceInfo, userAgent);
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

    public Room SetTrack(string connectionId, string url, string? title = null, string? sourceUrl = null, long? trackId = null)
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
            room.TrackSourceUrl = string.IsNullOrWhiteSpace(sourceUrl) ? null : sourceUrl.Trim();
            room.TrackTitle = string.IsNullOrWhiteSpace(title) ? null : title.Trim();
            room.TrackId = trackId;
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
                room.TrackSourceUrl,
                room.TrackTitle,
                room.TrackId,
                room.AllUsersReady,
                room.IsPlaybackScheduled,
                room.ScheduledStartAtUtc,
                users);
        }
    }

    private string GenerateRoomCode()
    {
        var codeLength = _rooms.Count > RoomCodeExpansionThreshold
            ? LongRoomCodeLength
            : ShortRoomCodeLength;
        var maxExclusive = (int)Math.Pow(10, codeLength);

        for (var attempt = 0; attempt < 64; attempt++)
        {
            var code = RandomNumberGenerator
                .GetInt32(0, maxExclusive)
                .ToString($"D{codeLength}");

            if (!_rooms.ContainsKey(code))
            {
                return code;
            }
        }

        for (var value = 0; value < maxExclusive; value++)
        {
            var code = value.ToString($"D{codeLength}");

            if (!_rooms.ContainsKey(code))
            {
                return code;
            }
        }

        throw new InvalidOperationException("Не удалось сгенерировать код комнаты.");
    }

    private static void AddUser(Room room, string connectionId, ClientDeviceInfo? deviceInfo, string? userAgent)
    {
        var suffixLength = Math.Min(4, connectionId.Length);
        var baseName = $"Слушатель {connectionId[^suffixLength..].ToUpperInvariant()}";
        var deviceName = GetDeviceName(deviceInfo, userAgent);
        var displayName = string.IsNullOrWhiteSpace(deviceName)
            ? baseName
            : $"{baseName} - {deviceName}";

        room.Users[connectionId] = new User
        {
            ConnectionId = connectionId,
            Name = displayName
        };
    }

    private static string? GetDeviceName(ClientDeviceInfo? deviceInfo, string? userAgent)
    {
        var deviceType = CleanDevicePart(deviceInfo?.DeviceType) ?? GetDeviceType(userAgent);
        var model = CleanDevicePart(deviceInfo?.Model) ?? GetModelName(userAgent);
        var os = CleanDevicePart(deviceInfo?.Os) ?? GetOsName(userAgent);

        if (model is not null &&
            deviceType is not null &&
            model.Equals(deviceType, StringComparison.OrdinalIgnoreCase))
        {
            model = null;
        }

        var device = string.Join(" ", new[] { deviceType, model }.Where(part => !string.IsNullOrWhiteSpace(part)));

        if (string.IsNullOrWhiteSpace(device))
        {
            return os;
        }

        return os is null ? device : $"{device} / {os}";
    }

    private static string? CleanDevicePart(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var cleaned = value.Trim().Trim('"');

        if (cleaned.Length == 0 ||
            cleaned.Equals("unknown", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return cleaned.Length > 80 ? cleaned[..80] : cleaned;
    }

    private static string? GetDeviceType(string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
        {
            return null;
        }

        if (userAgent.Contains("iPad", StringComparison.OrdinalIgnoreCase) ||
            userAgent.Contains("Tablet", StringComparison.OrdinalIgnoreCase))
        {
            return "Планшет";
        }

        if (userAgent.Contains("iPhone", StringComparison.OrdinalIgnoreCase))
        {
            return "Телефон";
        }

        if (userAgent.Contains("Android", StringComparison.OrdinalIgnoreCase))
        {
            return userAgent.Contains("Mobile", StringComparison.OrdinalIgnoreCase)
                ? "Телефон"
                : "Планшет";
        }

        if (userAgent.Contains("Windows", StringComparison.OrdinalIgnoreCase) ||
            userAgent.Contains("Macintosh", StringComparison.OrdinalIgnoreCase) ||
            userAgent.Contains("Mac OS X", StringComparison.OrdinalIgnoreCase) ||
            userAgent.Contains("Linux", StringComparison.OrdinalIgnoreCase))
        {
            return "Компьютер";
        }

        return "Устройство";
    }

    private static string? GetModelName(string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
        {
            return null;
        }

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
            return GetAndroidModelName(userAgent);
        }

        return null;
    }

    private static string? GetAndroidModelName(string userAgent)
    {
        foreach (var group in userAgent.Split('(', ')'))
        {
            if (!group.Contains("Android", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var parts = group
                .Split(';')
                .Select(part => part.Trim())
                .ToArray();
            var androidIndex = Array.FindIndex(parts, part => part.StartsWith("Android", StringComparison.OrdinalIgnoreCase));

            for (var index = androidIndex + 1; index < parts.Length; index += 1)
            {
                var model = CleanAndroidModel(parts[index]);

                if (!string.IsNullOrWhiteSpace(model))
                {
                    return model;
                }
            }
        }

        return null;
    }

    private static string? CleanAndroidModel(string value)
    {
        var model = value.Trim();
        var buildIndex = model.IndexOf(" Build", StringComparison.OrdinalIgnoreCase);

        if (buildIndex >= 0)
        {
            model = model[..buildIndex].Trim();
        }

        if (model.Length < 3 ||
            model.Equals("wv", StringComparison.OrdinalIgnoreCase) ||
            model.Equals("Mobile", StringComparison.OrdinalIgnoreCase) ||
            model.StartsWith("Version/", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return CleanDevicePart(model);
    }

    private static string? GetOsName(string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
        {
            return null;
        }

        if (userAgent.Contains("Android", StringComparison.OrdinalIgnoreCase))
        {
            var androidVersion = ReadVersionAfter(userAgent, "Android ");
            return string.IsNullOrWhiteSpace(androidVersion)
                ? "Android"
                : $"Android {androidVersion}";
        }

        if (userAgent.Contains("iPhone", StringComparison.OrdinalIgnoreCase) ||
            userAgent.Contains("iPad", StringComparison.OrdinalIgnoreCase))
        {
            var iosVersion = ReadVersionAfter(userAgent, "OS ")?.Replace('_', '.');
            return string.IsNullOrWhiteSpace(iosVersion)
                ? "iOS"
                : $"iOS {iosVersion}";
        }

        if (userAgent.Contains("Windows", StringComparison.OrdinalIgnoreCase))
        {
            return "Windows";
        }

        if (userAgent.Contains("Macintosh", StringComparison.OrdinalIgnoreCase) ||
            userAgent.Contains("Mac OS X", StringComparison.OrdinalIgnoreCase))
        {
            var macVersion = ReadVersionAfter(userAgent, "Mac OS X ")?.Replace('_', '.');
            return string.IsNullOrWhiteSpace(macVersion)
                ? "macOS"
                : $"macOS {macVersion}";
        }

        if (userAgent.Contains("Linux", StringComparison.OrdinalIgnoreCase))
        {
            return "Linux";
        }

        return null;
    }

    private static string? ReadVersionAfter(string value, string marker)
    {
        var startIndex = value.IndexOf(marker, StringComparison.OrdinalIgnoreCase);

        if (startIndex < 0)
        {
            return null;
        }

        startIndex += marker.Length;
        var endIndex = startIndex;

        while (endIndex < value.Length &&
            (char.IsDigit(value[endIndex]) || value[endIndex] == '.' || value[endIndex] == '_'))
        {
            endIndex += 1;
        }

        return endIndex == startIndex ? null : value[startIndex..endIndex];
    }

    private static string NormalizeRoomCode(string roomCode)
    {
        var normalizedCode = roomCode.Trim();

        if ((normalizedCode.Length != ShortRoomCodeLength && normalizedCode.Length != LongRoomCodeLength) ||
            !normalizedCode.All(char.IsDigit))
        {
            throw new InvalidOperationException("Код комнаты должен состоять из 3 или 4 цифр.");
        }

        return normalizedCode;
    }
}
