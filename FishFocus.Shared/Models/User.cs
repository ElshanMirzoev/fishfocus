namespace FishFocus.Shared.Models;

public class User
{
    //Данные пользователя
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public int TotalPoints { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Настройки пользователя
    public bool IsNightMode { get; set; } = false;
    public bool PlayFog { get; set; } = false;

    // Радио
    public bool IsRadioEnabled { get; set; } = false;
    public int RadioVolume { get; set; } = 50;

    // Дождь
    public bool IsRainEnabled { get; set; } = false;
    public int RainVolume { get; set; } = 50;

    // Птицы (будущее)
    public bool IsBirdsEnabled { get; set; } = false;
    public int BirdsVolume { get; set; } = 50;

    // Волны (будущее)
    public bool IsWavesEnabled { get; set; } = false;
    public int WavesVolume { get; set; } = 50;

    // Гром (будущее)
    public bool IsThunderEnabled { get; set; } = false;
    public int ThunderVolume { get; set; } = 50;

    // Минуты
    public int LastSelectedMinutes { get; set; }
    public string? AvatarData { get; set; }
    public List<FishCatchResult> CaughtFishes { get; set; } = new();
    public List<DiaryEntry> DiaryEntries { get; set; } = new();
}