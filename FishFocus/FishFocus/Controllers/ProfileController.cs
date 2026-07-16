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

    private int? GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(claim, out var id) ? id : null;
    }
}