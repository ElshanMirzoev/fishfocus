using FishFocus.Data;
using FishFocus.Shared.DTOs.Auth;
using FishFocus.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;

namespace FishFocus.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    public AuthService(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        var errors = new List<string>();

        // Проверка уникальности email
        if (await _db.Users.AnyAsync(u => u.Email == request.Email.ToLower()))
            errors.Add("Пользователь с таким email уже существует");

        // Проверка уникальности username
        if (await _db.Users.AnyAsync(u => u.Username == request.Username))
            errors.Add("Это имя пользователя уже занято");

        // Проверка сложности пароля
        if (!IsPasswordStrong(request.Password, out var passwordErrors))
            errors.AddRange(passwordErrors);

        if (errors.Count > 0)
            return new AuthResponse { Success = false, Errors = errors };

        var user = new User
        {
            Email = request.Email.ToLower().Trim(),
            Username = request.Username.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            TotalPoints = 0,
            CreatedAt = DateTime.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var token = GenerateJwt(user);

        return new AuthResponse
        {
            Success = true,
            Token = token,
            Username = user.Username,
            Email = user.Email,
            TotalPoints = user.TotalPoints
        };
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email.ToLower());

        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return new AuthResponse
            {
                Success = false,
                Errors = new List<string> { "Неверный email или пароль" }
            };
        }

        var token = GenerateJwt(user);

        return new AuthResponse
        {
            Success = true,
            Token = token,
            Username = user.Username,
            Email = user.Email,
            TotalPoints = user.TotalPoints
        };
    }

    private string GenerateJwt(User user)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.Username),

        };

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(
                double.Parse(_config["Jwt:ExpiresInMinutes"]!)),
            signingCredentials: new SigningCredentials(
                key, SecurityAlgorithms.HmacSha256)
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static bool IsPasswordStrong(string password, out List<string> errors)
    {
        errors = new List<string>();

        if (password.Length < 8)
            errors.Add("Пароль должен содержать минимум 8 символов");
        if (!Regex.IsMatch(password, "[A-Z]"))
            errors.Add("Пароль должен содержать хотя бы одну заглавную букву");
        if (!Regex.IsMatch(password, "[a-z]"))
            errors.Add("Пароль должен содержать хотя бы одну строчную букву");
        if (!Regex.IsMatch(password, "[0-9]"))
            errors.Add("Пароль должен содержать хотя бы одну цифру");

        return errors.Count == 0;
    }
}