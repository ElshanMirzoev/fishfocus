namespace FishFocus.Shared.DTOs.Profile;

public class PublicProfileDto
{
    public string Username { get; set; } = string.Empty;
    public string? AvatarData { get; set; }
    public int TotalPoints { get; set; }
    public int TotalSessions { get; set; }
    public int TotalMinutesFished { get; set; }
    public string FavoriteFish { get; set; } = "Нет улова";
    public int StreakDays { get; set; }
}
