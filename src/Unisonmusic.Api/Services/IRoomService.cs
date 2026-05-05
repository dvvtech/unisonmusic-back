using Unisonmusic.Api.Models;
using Unisonmusic.Api.Models.Dtos;

namespace Unisonmusic.Api.Services;

public interface IRoomService
{
    Room CreateRoom(string connectionId);
    Room JoinRoom(string roomCode, string connectionId);
    bool TryLeave(string connectionId, out Room? previousRoom);
    Room GetRoomForConnection(string connectionId);
    Room SetTrack(string connectionId, string url);
    (Room Room, bool AllReady) SetReady(string connectionId);
    PlaybackCommand SchedulePlayback(Room room);
    IReadOnlyCollection<RoomSnapshotDto> GetSnapshots();
    RoomSnapshotDto GetSnapshot(string roomCode);
    RoomSnapshotDto ToSnapshot(Room room);
}
