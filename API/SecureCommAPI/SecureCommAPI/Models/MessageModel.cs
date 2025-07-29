using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SecureCommAPI.Models;

[Table("messages")]
public class MessageModel
{
    [Column("id")]
    public Guid Id { get; set; }

    [Column("room_id")]
    public Guid RoomId { get; set; }

    [Column("user_id")]
    public string UserId { get; set; }

    [Column("content")]
    public string Content { get; set; }

    [Column("color")]
    public string Color { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
}