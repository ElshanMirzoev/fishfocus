using FishFocus.Shared.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace FishFocus.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    public DbSet<User> Users { get; set; }

    public DbSet<Fish> Fishes { get; set; }
    public DbSet<FishCatchResult> CaughtFishes { get; set; }
    public DbSet<DiaryEntry> DiaryEntries { get; set; }
    public DbSet<Feedback> Feedbacks { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Email).IsUnique();
            entity.HasIndex(u => u.Username).IsUnique();

            entity.Property(u => u.Email).HasMaxLength(256).IsRequired();
            entity.Property(u => u.Username).HasMaxLength(30).IsRequired();
            entity.Property(u => u.PasswordHash).IsRequired();
        });
    }
}