using System;

namespace TicketVoidingSystem.Models;

public class NotificationModel
{
    public int Id { get; set; }
    public int Username { get; set; }
    public string Content { get; set; }
    public DateTime CreatedDate { get; set; }
    public bool IsRead { get; set; }
    public string Type { get; set; }
}