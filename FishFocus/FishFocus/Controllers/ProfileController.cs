using FishFocus.Data;
using FishFocus.Shared.DTOs.Profile;
using FishFocus.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FishFocus.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProfileController : ControllerBase
{
    private readonly AppDbContext _db;

    public ProfileController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetProfile()
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var user = await _db.Users
            .Include(u => u.CaughtFishes)
            .ThenInclude(f => f.Fish)
            .Include(u => u.DiaryEntries)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user is null) return NotFound();

        return Ok(new UserProfileDto
        {
            Username = user.Username,
            Email = user.Email,
            TotalPoints = user.TotalPoints,
            CreatedAt = user.CreatedAt,
            IsNightMode = user.IsNightMode,
            PlayFog = user.PlayFog,
            IsRadioEnabled = user.IsRadioEnabled,
            RadioVolume = user.RadioVolume,
            IsRainEnabled = user.IsRainEnabled,
            RainVolume = user.RainVolume,
            IsBirdsEnabled = user.IsBirdsEnabled,
            BirdsVolume = user.BirdsVolume,
            IsWavesEnabled = user.IsWavesEnabled,
            WavesVolume = user.WavesVolume,
            IsThunderEnabled = user.IsThunderEnabled,
            ThunderVolume = user.ThunderVolume,
            LastSelectedMinutes = user.LastSelectedMinutes,
            AvatarData = user.AvatarData,

            CaughtFishes = user.CaughtFishes,
            DiaryEntries = user.DiaryEntries
        });
    }

    [HttpPost("save-catch")]
    public async Task<IActionResult> SaveCatch([FromBody] FishCatchResult catchResult)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        catchResult.UserId = userId.Value;
        _db.CaughtFishes.Add(catchResult);

        var user = await _db.Users.FindAsync(userId.Value);
        if (user is null) return NotFound();
        user.TotalPoints += catchResult.TotalPoints;

        await _db.SaveChangesAsync();
        return Ok(new { user.TotalPoints });
    }

    [HttpPost("save-diary")]
    public async Task<IActionResult> SaveDiary([FromBody] DiaryEntry entry)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        try
        {
            var user = await _db.Users
                .Include(u => u.DiaryEntries)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null) return NotFound();

            var newEntry = new DiaryEntry
            {
                UserId = userId.Value,
                CompletionTime = DateTime.UtcNow,
                MinutesSpent = entry.MinutesSpent,
                FishName = entry.FishName ?? "Рыба",
                Note = entry.Note ?? ""
            };

            user.DiaryEntries.Add(newEntry);
            await _db.SaveChangesAsync();

            return Ok();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR SaveDiary]: {ex.Message}");
            return StatusCode(500, ex.Message);
        }
    }

    [HttpPut("settings")]
    public async Task<IActionResult> UpdateSettings([FromBody] UpdateSettingsRequest req)
    {
        var userId = GetUserId();
        var user = await _db.Users.FindAsync(userId);
        if (user is null) return NotFound();

        user.IsNightMode = req.IsNightMode;
        user.PlayFog = req.PlayFog;
        user.IsRadioEnabled = req.IsRadioEnabled;
        user.RadioVolume = Math.Clamp(req.RadioVolume, 0, 100);
        user.IsRainEnabled = req.IsRainEnabled;
        user.RainVolume = Math.Clamp(req.RainVolume, 0, 100);
        user.IsBirdsEnabled = req.IsBirdsEnabled;
        user.BirdsVolume = Math.Clamp(req.BirdsVolume, 0, 100);
        user.IsWavesEnabled = req.IsWavesEnabled;
        user.WavesVolume = Math.Clamp(req.WavesVolume, 0, 100);
        user.IsThunderEnabled = req.IsThunderEnabled;
        user.ThunderVolume = Math.Clamp(req.ThunderVolume, 0, 100);
        user.LastSelectedMinutes = req.LastSelectedMinutes;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("leaderboard")]
    [AllowAnonymous]
    public async Task<IActionResult> GetLeaderboard()
    {
        var leaders = await _db.Users
            .Where(u => u.TotalPoints > 0)
            .OrderByDescending(u => u.TotalPoints)
            .Take(50)
            .Select(u => new LeaderboardEntryDto
            {
                Username = u.Username,
                TotalPoints = u.TotalPoints
            })
            .ToListAsync();

        return Ok(leaders);
    }

    [HttpPost("feedback")]
    public async Task<IActionResult> SubmitFeedback([FromBody] Feedback feedback)
    {
        var userId = GetUserId();
        if (userId is not null)
        {
            feedback.UserId = userId.Value;
            var user = await _db.Users.FindAsync(userId.Value);
            if (user != null)
            {
                feedback.Username = user.Username;
            }
        }
        feedback.CreatedAt = DateTime.UtcNow;
        _db.Feedbacks.Add(feedback);
        await _db.SaveChangesAsync();
        return Ok();
    }

    [HttpPost("avatar")]
    public async Task<IActionResult> UpdateAvatar([FromBody] AvatarUpdateRequest req)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var user = await _db.Users.FindAsync(userId.Value);
        if (user is null) return NotFound();

        user.AvatarData = req.AvatarData;
        await _db.SaveChangesAsync();
        return Ok(new { AvatarData = user.AvatarData });
    }

    [HttpGet("public/{username}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPublicProfile(string username)
    {
        var user = await _db.Users
            .Include(u => u.CaughtFishes)
            .ThenInclude(f => f.Fish)
            .Include(u => u.DiaryEntries)
            .FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());

        if (user is null) return NotFound();

        var totalSessions = user.DiaryEntries?.Count ?? 0;
        var totalMinutes = user.DiaryEntries?.Sum(d => d.MinutesSpent) ?? 0;
        
        var favoriteFish = "Нет улова";
        if (user.CaughtFishes != null && user.CaughtFishes.Any())
        {
            var fav = user.CaughtFishes
                .Where(c => c.Fish != null && !string.IsNullOrEmpty(c.Fish.Name))
                .GroupBy(c => c.Fish.Name)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault();
            if (fav != null)
            {
                favoriteFish = $"{fav.Key} ({fav.Count()} шт.)";
            }
        }

        var streak = CalculateStreak(user.DiaryEntries ?? new());

        return Ok(new PublicProfileDto
        {
            Username = user.Username,
            AvatarData = user.AvatarData,
            TotalPoints = user.TotalPoints,
            TotalSessions = totalSessions,
            TotalMinutesFished = totalMinutes,
            FavoriteFish = favoriteFish,
            StreakDays = streak
        });
    }

    private int CalculateStreak(List<DiaryEntry> entries)
    {
        if (entries == null || !entries.Any()) return 0;
        
        var dates = entries
            .Where(e => e != null)
            .Select(e => e.CompletionTime.Date)
            .Distinct()
            .OrderByDescending(d => d)
            .ToList();
            
        if (!dates.Any()) return 0;
        
        var today = DateTime.UtcNow.Date;
        var yesterday = today.AddDays(-1);
        
        if (dates[0] != today && dates[0] != yesterday)
        {
            return 0;
        }
        
        int streak = 0;
        var currentCheckDate = dates[0];
        
        for (int i = 0; i < dates.Count; i++)
        {
            if (dates[i] == currentCheckDate)
            {
                streak++;
                currentCheckDate = currentCheckDate.AddDays(-1);
            }
            else if (dates[i] < currentCheckDate)
            {
                break;
            }
        }
        
        return streak;
    }

    public class AvatarUpdateRequest
    {
        public string? AvatarData { get; set; }
    }

    private int? GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(claim, out var id) ? id : null;
    }
}
