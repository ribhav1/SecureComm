using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecureCommAPI.Models;

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

            return null;
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
}