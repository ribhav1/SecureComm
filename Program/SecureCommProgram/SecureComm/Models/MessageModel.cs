using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SecureCommAPI.Models;

public class MessageModel
{
    public Guid Id { get; set; }

    public Guid RoomId { get; set; }

    public Guid UserId { get; set; }

    public string Username { get; set; }

    public string Content { get; set; }

    public Guid? DirectlyTo { get; set; }

    public string Color { get; set; }

    public DateTime CreatedAt { get; set; }
}