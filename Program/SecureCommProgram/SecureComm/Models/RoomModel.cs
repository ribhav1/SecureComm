﻿using System.ComponentModel.DataAnnotations.Schema;

namespace SecureCommAPI.Models;

public class RoomModel
{
    public Guid Id { get; set; }

    public string Password { get; set; }

    public DateTime CreatedAt { get; set; }
}