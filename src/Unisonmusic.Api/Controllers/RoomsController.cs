using Microsoft.AspNetCore.Mvc;
using Unisonmusic.Api.Models.Dtos;
using Unisonmusic.Api.Services;

namespace Unisonmusic.Api.Controllers;

[ApiController]
[Route("rooms")]
public sealed class RoomsController : ControllerBase
{
    private readonly IRoomService _roomService;

    public RoomsController(IRoomService roomService)
    {
        _roomService = roomService;
    }

    [HttpGet]
    public ActionResult<IReadOnlyCollection<RoomSnapshotDto>> GetRooms()
    {
        return Ok(_roomService.GetSnapshots());
    }

    [HttpGet("{roomCode}")]
    public ActionResult<RoomSnapshotDto> GetRoom(string roomCode)
    {
        try
        {
            return Ok(_roomService.GetSnapshot(roomCode));
        }
        catch (InvalidOperationException exception)
        {
            return NotFound(new { message = exception.Message });
        }
    }

    [HttpGet("test")]
    public ActionResult<string> Test()
    {
        return Ok("579");
    }
}
