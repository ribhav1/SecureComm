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

            return BadRequest("Could not add user");
        }
    }

    [HttpPost("removeConnectedUser/{roomGUID}/{newConnectedUserId}")]
    public async Task<IActionResult> RemoveConnectedUser(Guid roomGUID, Guid newConnectedUserId)
    {
        try
        {
            RoomModel targetRoom = await db_context.Rooms.SingleOrDefaultAsync(room => room.Id == roomGUID);
            if (targetRoom == null)
                return NotFound("Room not found");

            var updatedUsers = new Dictionary<Guid, string>(targetRoom.ConnectedUsers ?? new());
            updatedUsers.Remove(newConnectedUserId);
            targetRoom.ConnectedUsers = updatedUsers;

            // if there are no more users in the room, delete the room and all of its messages
            if (targetRoom.ConnectedUsers.Count == 0)
            {
                await new MessageController(db_context).DeleteRoomMessages(roomGUID); // messages have to be deleted first because of foreign key reference
                await DeleteRoom(roomGUID);

                return Ok("No connected users detected. Room and all messages deleted");
            }

            await db_context.SaveChangesAsync();

            return Ok(targetRoom.ConnectedUsers);
        }
        catch (Exception e)
        {
            Console.WriteLine("EXCEPTION CAUGHT: " + e.Message);

            return BadRequest("Could not remove user");
        }
    }

    [HttpDelete("deleteRoom/{roomGUID}")]
    public async Task<IActionResult> DeleteRoom(Guid roomGUID)
    {
        try
        {
            await db_context.Rooms.Where(room => room.Id == roomGUID).ExecuteDeleteAsync();
            await db_context.SaveChangesAsync();

            return Ok("Deleted room sucessfully");
        }
        catch (Exception e)
        {
            Console.WriteLine("EXCEPTION CAUGHT: " + e.Message);

            return BadRequest("Could not delete room");
        }
    }
}