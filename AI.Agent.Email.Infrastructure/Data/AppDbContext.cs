using AI.Agent.Email.Core.Entities;
using AI.Agent.Email.Core.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace AI.Agent.Email.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<global::AI.Agent.Email.Core.Entities.Email> Emails => Set<global::AI.Agent.Email.Core.Entities.Email>();
    public DbSet<TaskItem> Tasks => Set<TaskItem>();
    public DbSet<AgentLog> AgentLogs => Set<AgentLog>();
    public DbSet<EmailAccount> EmailAccounts => Set<EmailAccount>();
    public DbSet<UserSettings> UserSettings => Set<UserSettings>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User Configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Email).HasMaxLength(255).IsRequired();
            entity.Property(e => e.PasswordHash).HasMaxLength(500).IsRequired();
            entity.Property(e => e.FirstName).HasMaxLength(100);
            entity.Property(e => e.LastName).HasMaxLength(100);
            entity.Property(e => e.RefreshToken).HasMaxLength(500);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();
        });

        // Email Configuration
        modelBuilder.Entity<global::AI.Agent.Email.Core.Entities.Email>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.GmailMessageId).IsUnique();
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.IsProcessed);
            entity.Property(e => e.GmailMessageId).HasMaxLength(255).IsRequired();
            entity.Property(e => e.Subject).HasMaxLength(500);
            entity.Property(e => e.SenderEmail).HasMaxLength(255).IsRequired();
            entity.Property(e => e.SenderName).HasMaxLength(200);
            entity.Property(e => e.Body).HasColumnType("text");
            entity.Property(e => e.BodyPlainText).HasColumnType("text");
            entity.Property(e => e.Priority).HasMaxLength(50);
            entity.Property(e => e.SuggestedAction).HasMaxLength(100);
            entity.Property(e => e.TaskTitle).HasMaxLength(500);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();

            entity.HasOne(e => e.User)
                .WithMany(u => u.Emails)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.EmailAccount)
                .WithMany(a => a.Emails)
                .HasForeignKey(e => e.EmailAccountId)
                .OnDelete(DeleteBehavior.Restrict);

            // Enum conversions
            entity.Property(e => e.Category)
                .HasConversion<string>()
                .HasMaxLength(50);
            entity.Property(e => e.Intent)
                .HasConversion<string>()
                .HasMaxLength(50);
            entity.Property(e => e.Status)
                .HasConversion<string>()
                .HasMaxLength(50);
        });

        // TaskItem Configuration
        modelBuilder.Entity<TaskItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.EmailId);
            entity.Property(e => e.Title).HasMaxLength(500).IsRequired();
            entity.Property(e => e.Description).HasColumnType("text");
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();

            entity.HasOne(e => e.User)
                .WithMany(u => u.Tasks)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Email)
                .WithMany(e => e.Tasks)
                .HasForeignKey(e => e.EmailId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.Property(e => e.Status)
                .HasConversion<string>()
                .HasMaxLength(50);
        });

        // AgentLog Configuration
        modelBuilder.Entity<AgentLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.EmailId);
            entity.HasIndex(e => e.Timestamp);
            entity.Property(e => e.ActionTaken).HasMaxLength(200).IsRequired();
            entity.Property(e => e.ActionDetails).HasColumnType("text");
            entity.Property(e => e.ErrorMessage).HasColumnType("text");
            entity.Property(e => e.Timestamp).IsRequired();

            entity.HasOne(e => e.Email)
                .WithMany(e => e.AgentLogs)
                .HasForeignKey(e => e.EmailId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // EmailAccount Configuration
        modelBuilder.Entity<EmailAccount>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.UserId, e.Provider });
            entity.Property(e => e.Provider).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Email).HasMaxLength(255).IsRequired();
            entity.Property(e => e.AccessToken).HasMaxLength(2000);
            entity.Property(e => e.RefreshToken).HasMaxLength(2000);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();

            entity.HasOne(e => e.User)
                .WithMany(u => u.EmailAccounts)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // UserSettings Configuration
        modelBuilder.Entity<UserSettings>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId).IsUnique();
            entity.Property(e => e.DefaultReplyLanguage).HasMaxLength(10);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();

            entity.HasOne(e => e.User)
                .WithOne(u => u.Settings)
                .HasForeignKey<UserSettings>(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
