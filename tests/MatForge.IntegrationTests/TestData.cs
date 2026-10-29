using MatForge.Data;
using MatForge.Data.Entities;
using MatForge.Data.Enums;
using Microsoft.EntityFrameworkCore;

namespace MatForge.IntegrationTests;

// Builds rows for one user directly in the database (one row in every user table)
public static class TestData
{
    public static async Task AddUserWithFullDataAsync(AppDbContext db, Guid userId)
    {
        if (!await db.Users.AnyAsync(u => u.Id == userId))
        {
            db.Users.Add(new AppUser { Id = userId, UserName = $"{userId}@test.local", Email = $"{userId}@test.local" });
        }

        var bjj = await db.Disciplines.SingleAsync(d => d.Code == "bjj");
        var gi = await db.DisciplineFormats.SingleAsync(f => f.DisciplineId == bjj.Id && f.Code == "gi");
        var sparring = await db.TrainingCharacters.SingleAsync(c => c.Code == "sparring");
        var armbar = await db.Techniques.SingleAsync(t => t.Code == "armbar");
        var mount = await db.Positions.SingleAsync(p => p.Code == "mount");
        var whiteBelt = await db.Belts.SingleAsync(b => b.DisciplineId == bjj.Id && b.Code == "white");
        var now = DateTime.UtcNow;

        var club = new Club { UserId = userId, Name = "Test Club", CreatedAtUtc = now };
        var competition = new Competition
        {
            UserId = userId, Name = "Test Open", Date = new DateOnly(2026, 9, 12), CreatedAtUtc = now,
            DisciplineId = bjj.Id, FormatId = gi.Id,
            Fights = [new Fight { Order = 1, Result = FightResult.Win, Method = FightMethod.Submission, TechniqueId = armbar.Id }]
        };

        db.AddRange(
            club,
            competition,
            new TrainingSession
            {
                UserId = userId, Date = new DateOnly(2026, 10, 1), Kind = SessionKind.Grappling, CreatedAtUtc = now,
                DurationMinutes = 90, Feeling = 4, DisciplineId = bjj.Id, FormatId = gi.Id, CharacterId = sparring.Id, Club = club,
                Rounds = [new SparringRound { Order = 1, Result = RoundResult.Win, TechniqueId = armbar.Id, PositionId = mount.Id }]
            },
            new Goal { UserId = userId, Type = GoalType.CompetitionPrep, Title = "Prepare", Competition = competition, CreatedAtUtc = now },
            new UserBelt { UserId = userId, DisciplineId = bjj.Id, BeltId = whiteBelt.Id, Degree = 2, CreatedAtUtc = now },
            new Material { UserId = userId, Title = "Armbar details", Url = "https://youtube.com/watch?v=1", CreatedAtUtc = now },
            new BodyMeasurement { UserId = userId, Date = new DateOnly(2026, 10, 1), WeightKg = 80.5m, CreatedAtUtc = now });

        await db.SaveChangesAsync();
    }
}
