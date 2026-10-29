using MatForge.Data.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace MatForge.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options, ICurrentUser currentUser)
    : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>(options)
{
    private Guid? CurrentUserId => currentUser.UserId;

    public DbSet<Discipline> Disciplines => Set<Discipline>();
    public DbSet<DisciplineFormat> DisciplineFormats => Set<DisciplineFormat>();
    public DbSet<TrainingCharacter> TrainingCharacters => Set<TrainingCharacter>();
    public DbSet<ActivityType> ActivityTypes => Set<ActivityType>();
    public DbSet<TechniqueCategory> TechniqueCategories => Set<TechniqueCategory>();
    public DbSet<TechniqueCategoryDiscipline> TechniqueCategoryDisciplines => Set<TechniqueCategoryDiscipline>();
    public DbSet<Technique> Techniques => Set<Technique>();
    public DbSet<Position> Positions => Set<Position>();
    public DbSet<PositionDiscipline> PositionDisciplines => Set<PositionDiscipline>();
    public DbSet<Belt> Belts => Set<Belt>();
    public DbSet<MaterialCategory> MaterialCategories => Set<MaterialCategory>();

    public DbSet<Club> Clubs => Set<Club>();
    public DbSet<UserBelt> UserBelts => Set<UserBelt>();
    public DbSet<TrainingSession> TrainingSessions => Set<TrainingSession>();
    public DbSet<SparringRound> SparringRounds => Set<SparringRound>();
    public DbSet<Competition> Competitions => Set<Competition>();
    public DbSet<Fight> Fights => Set<Fight>();
    public DbSet<Goal> Goals => Set<Goal>();
    public DbSet<Material> Materials => Set<Material>();
    public DbSet<BodyMeasurement> BodyMeasurements => Set<BodyMeasurement>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Enums are stored as text
        configurationBuilder.Properties<Enum>().HaveConversion<string>().HaveMaxLength(30);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // TrainingSession -> SparringRounds (1:N), rounds are deleted with their session
        modelBuilder.Entity<TrainingSession>()
            .HasMany(s => s.Rounds)
            .WithOne(r => r.TrainingSession)
            .HasForeignKey(r => r.TrainingSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Competition -> Fights (1:N), fights are deleted with their competition
        modelBuilder.Entity<Competition>()
            .HasMany(c => c.Fights)
            .WithOne(f => f.Competition)
            .HasForeignKey(f => f.CompetitionId)
            .OnDelete(DeleteBehavior.Cascade);

        // TrainingSession -> Club (N:1), a club used by a session cannot be deleted
        modelBuilder.Entity<TrainingSession>()
            .HasOne(s => s.Club)
            .WithMany()
            .HasForeignKey(s => s.ClubId)
            .OnDelete(DeleteBehavior.Restrict);

        // Goal -> Competition (N:1), a competition used by a goal cannot be deleted
        modelBuilder.Entity<Goal>()
            .HasOne(g => g.Competition)
            .WithMany()
            .HasForeignKey(g => g.CompetitionId)
            .OnDelete(DeleteBehavior.Restrict);

        // Grappling sessions and other sessions require different fields
        modelBuilder.Entity<TrainingSession>()
            .ToTable(t => t.HasCheckConstraint("CK_TrainingSessions_Kind",
                "([Kind] = 'Grappling' AND [DisciplineId] IS NOT NULL AND [FormatId] IS NOT NULL AND [CharacterId] IS NOT NULL AND [ActivityTypeId] IS NULL)" +
                " OR ([Kind] = 'Other' AND [ActivityTypeId] IS NOT NULL AND [DisciplineId] IS NULL AND [FormatId] IS NULL AND [CharacterId] IS NULL AND [ClubId] IS NULL)"));

        // Dictionaries are never deleted, so references to them must not cascade
        var dictionaryForeignKeys = modelBuilder.Model.GetEntityTypes()
            .SelectMany(e => e.GetForeignKeys())
            .Where(fk => fk.PrincipalEntityType.ClrType.IsSubclassOf(typeof(DictionaryEntity)));
        foreach (var foreignKey in dictionaryForeignKeys)
        {
            foreignKey.DeleteBehavior = DeleteBehavior.Restrict;
        }

        // Every user sees only their own data (no rows when nobody is logged in)
        modelBuilder.Entity<Club>().HasQueryFilter(e => e.UserId == CurrentUserId);
        modelBuilder.Entity<UserBelt>().HasQueryFilter(e => e.UserId == CurrentUserId);
        modelBuilder.Entity<TrainingSession>().HasQueryFilter(e => e.UserId == CurrentUserId);
        modelBuilder.Entity<Competition>().HasQueryFilter(e => e.UserId == CurrentUserId);
        modelBuilder.Entity<Goal>().HasQueryFilter(e => e.UserId == CurrentUserId);
        modelBuilder.Entity<Material>().HasQueryFilter(e => e.UserId == CurrentUserId);
        modelBuilder.Entity<BodyMeasurement>().HasQueryFilter(e => e.UserId == CurrentUserId);

        // Rounds and fights have no UserId, so they are filtered through their parent
        modelBuilder.Entity<SparringRound>().HasQueryFilter(r => r.TrainingSession.UserId == CurrentUserId);
        modelBuilder.Entity<Fight>().HasQueryFilter(f => f.Competition.UserId == CurrentUserId);
    }
}
