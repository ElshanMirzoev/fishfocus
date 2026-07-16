using FishFocus.Shared.Models;

namespace FishFocus.Shared.DTOs.Profile;

public class UserProfileDto
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int TotalPoints { get; set; }
    public DateTime CreatedAt { get; set; }

    // Визуальные настройки
    public bool IsNightMode { get; set; }
    public bool PlayFog { get; set; }

    // Радио
    public bool IsRadioEnabled { get; set; }
    public int RadioVolume { get; set; }

    // Дождь
    public bool IsRainEnabled { get; set; }
    public int RainVolume { get; set; }

    // Птицы
    public bool IsBirdsEnabled { get; set; }
    public int BirdsVolume { get; set; }

    // Волны
    public bool IsWavesEnabled { get; set; }
    public int WavesVolume { get; set; }

    // Гром
    public bool IsThunderEnabled { get; set; }
    public int ThunderVolume { get; set; }

    // Cчетчик минут
    public int LastSelectedMinutes { get; set; }
    public string? AvatarData { get; set; }

    public List<FishCatchResult> CaughtFishes { get; set; } = new();
    public List<DiaryEntry> DiaryEntries { get; set; } = new();
}
