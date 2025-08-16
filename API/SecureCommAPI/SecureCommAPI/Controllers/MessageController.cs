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

    [HttpGet("getMessages/{roomGUID}/{lastCheckedTime}")]
    public async Task<List<MessageModel>> GetMessages(Guid roomGUID, DateTime lastCheckedTime, Guid? directRecepientUserId)
    {
        try
        {
            // only return message if:
            // it is the right room
            // it was sent after the last time messages were returned from the db
            // -> if there is no direct recipient specified in the query, then if the direct reciepient property is null
            // -> if there is a direct reciepent specified in the query, then if the the direct recipeint property matches the query param
            Guid? directlyTo = directRecepientUserId ?? null;
            
            return await db_context.Messages
                .Where(message => 
                    message.RoomId == roomGUID && 
                    message.CreatedAt > lastCheckedTime && 
                    message.DirectlyTo == (directRecepientUserId == null ? null : directRecepientUserId))
                .ToListAsync();
        }
        catch (Exception e)
        {
            Console.WriteLine("EXCEPTION CAUGHT: " + e);

            return new List<MessageModel>();
        }
    }

    [HttpPost("send/{roomGUID}/{sentByUserId}/{username}/{messageContent}/{color}")]
    public async Task<IActionResult> SendMessage(Guid roomGUID, Guid sentByUserId, string username, string messageContent, Guid? directlyToUserId, string color)
    {
        try
        {
            // create a message in the db with all the params specified in the query, and:
            // -> if there is no direct recepient specified, then set the directly to property as null
            // -> if there is a direct recepient specified, then set the directlt to property as the direct recepient query param
            MessageModel message = new MessageModel
            {
                Id = Guid.NewGuid(),
                RoomId = roomGUID,
                UserId = sentByUserId,
                Username = username,
                Content = messageContent,
                DirectlyTo = directlyToUserId == null ? null : directlyToUserId,
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

    [HttpDelete("deleteRoomMessages/{roomGUID}")]
    public async Task<IActionResult> DeleteRoomMessages(Guid roomGUID)
    {
        try
        {
            await db_context.Messages.Where(message => message.RoomId == roomGUID).ExecuteDeleteAsync();
            await db_context.SaveChangesAsync();

            return Ok("Deleted room messages sucessfully");
        }
        catch (Exception e)
        {
            Console.WriteLine("EXCEPTION CAUGHT: " + e);

            return BadRequest("Failed to delete room messages");
        }
    }
}
