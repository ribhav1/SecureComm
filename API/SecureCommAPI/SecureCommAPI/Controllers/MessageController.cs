using System.Drawing;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecureCommAPI.Models;

namespace SecureCommAPI.Controllers;

[Route("[controller]")]
[ApiController]
public class MessageController : ControllerBase
{
    private readonly SecureCommDbContext db_context;

    public MessageController(SecureCommDbContext context)
    {
        db_context = context;
    }

    [HttpGet("getMessages/{room_id}/{lastCheckedTime}")]
    public async Task<List<MessageModel>> GetMessages(Guid room_id, DateTime lastCheckedTime)
    {
        try
        {
            return await db_context.Messages.Where(message => (message.RoomId == room_id && message.CreatedAt > lastCheckedTime)).ToListAsync();
        }
        catch (Exception e)
        {
            Console.WriteLine("EXCEPTION CAUGHT: " + e);
            return new List<MessageModel>();
        }
    }

    [HttpPost("send/{room_id}/{user_id}/{message_content}/{color}")]
    public async Task<IActionResult> SendMessage(Guid room_id, string user_id, string message_content, string color)
    {
        try
        {
            MessageModel message = new MessageModel
            {
                Id = Guid.NewGuid(),
                RoomId = room_id,
                UserId = user_id,
                Content = message_content,
                Color = color,
                CreatedAt = DateTime.UtcNow,
            };
            db_context.Messages.Add(message);
            await db_context.SaveChangesAsync();

            return Ok(message);
        }
        catch (Exception e)
        {
            Console.WriteLine("EXCEPTION CAUGHT: " + e);
            return BadRequest("Failed");
        }

    }
}
