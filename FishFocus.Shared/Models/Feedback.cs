using System;
using System.ComponentModel.DataAnnotations;

namespace FishFocus.Shared.Models;

public class Feedback
{
    [Key]
    public int Id { get; set; }
    public int? UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FeedbackType { get; set; } = string.Empty; // e.g. "Пожелание", "Совет", "Ошибка", "Другое"
    public string Text { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
