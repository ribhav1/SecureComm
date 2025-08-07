using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecureCommAPI.Models;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Xml.Serialization;

namespace SecureCommAPI.Controllers;

[ApiController]
[Route("[controller]")]
public class RoomController : ControllerBase
{
    private readonly SecureCommDbContext db_context;

    public RoomController(SecureCommDbContext context)
    {
        db_context = context;
    }

    [HttpGet("getRoom/{id}")]
    public async Task<RoomModel> GetRoomById(Guid id)
    {
        try
        {
            return await db_context.Rooms.SingleOrDefaultAsync(room => room.Id == id);
        }
        catch (Exception e)
        {
            Console.WriteLine("EXCEPTION CAUGHT: " + e.Message);

            return new RoomModel();
        }
    }

    [HttpGet("validateRoom/{id}")]
    public async Task<Boolean> ValidateRoomById(Guid id)
    {
        try
        {
            return await db_context.Rooms.SingleOrDefaultAsync(room => room.Id == id) != null;
        }
        catch (Exception e)
        {
            Console.WriteLine("EXCEPTION CAUGHT: " + e.Message);

            return false;
        }
    }

    [HttpGet("validatePassword/{id}/{password}")]
    public async Task<Boolean> ValidateRoomPasswordById(Guid id, string password)
    {
        try
        {
            RoomModel targetRoom = await db_context.Rooms.FindAsync(id);

            return password == targetRoom.Password;
        }
        catch (Exception e)
        {
            Console.WriteLine("EXCEPTION CAUGHT: " + e.Message);

            return false;
        }
    }

    [HttpPost("createRoom/{roomGUID}/{password}")]
    public async Task<IActionResult> CreateRoom(Guid roomGUID, string password)
    {
        try
        {
            RoomModel room = new RoomModel
            {
                Id = roomGUID,
                Password = password,
                ConnectedUsers = new Dictionary<Guid, string>(),
                CreatedAt = DateTime.UtcNow
            };
            db_context.Rooms.Add(room);
            await db_context.SaveChangesAsync();

            return Ok(room);
        }
        catch (Exception e)
        {
            Console.WriteLine("EXCEPTION CAUGHT: " + e);

            return BadRequest("Failed");
        }
    }

    [HttpPost("addConnectedUser/{roomGUID}/{newConnectedUserId}/{newConnectedUserPublicKey}")]
    public async Task<IActionResult> AddConnectedUser(Guid roomGUID, Guid newConnectedUserId, string newConnectedUserPublicKey)
    {
        try
        {
            RoomModel targetRoom = await db_context.Rooms.SingleOrDefaultAsync(room => room.Id == roomGUID);
            if (targetRoom == null)
                return NotFound("Room not found");

            var updatedUsers = new Dictionary<Guid, string>(targetRoom.ConnectedUsers ?? new())
            {
                [newConnectedUserId] = newConnectedUserPublicKey
            };
            targetRoom.ConnectedUsers = updatedUsers;

            await db_context.SaveChangesAsync();

            return Ok(targetRoom.ConnectedUsers);
        }
        catch (Exception e)
        {
            Console.WriteLine("EXCEPTION CAUGHT: " + e);

            return BadRequest("Failed");
        }
    }

}