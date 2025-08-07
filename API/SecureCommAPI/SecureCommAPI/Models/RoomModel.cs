using System.ComponentModel.DataAnnotations.Schema;
using System.Security.Cryptography;

namespace SecureCommAPI.Models;

[Table("rooms")]
public class RoomModel
{
    [Column("id")]
    public Guid Id { get; set; }

    [Column("password")]
    public string Password { get; set; }

    [Column("connected_users")]
    public Dictionary<Guid, string> ConnectedUsers { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
}