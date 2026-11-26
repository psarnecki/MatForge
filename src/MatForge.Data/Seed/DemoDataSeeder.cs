using MatForge.Data.Entities;
using Microsoft.AspNetCore.Identity;

namespace MatForge.Data.Seed;

// Creates the demo user with a generated history, but only when that user does not exist yet
public class DemoDataSeeder(AppDbContext db, UserManager<AppUser> userManager)
{
    public const string Email = "demo@matforge.com";
    private const string Password = "Demo1234!";

    public async Task SeedAsync(DateOnly endDate, CancellationToken cancellationToken = default)
    {
        if (await userManager.FindByEmailAsync(Email) != null)
        {
            return;
        }

        var data = DemoDataGenerator.Generate(endDate, await DemoDictionary.LoadAsync(db, cancellationToken));

        // The user and the data are saved together, so a failed start never leaves a user without data
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var user = new AppUser { UserName = Email, Email = Email, FirstName = "Alex", LastName = "Nowak" };
        var result = await userManager.CreateAsync(user, Password);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"Demo user could not be created: {string.Join(" ", result.Errors.Select(e => e.Description))}");
        }

        var now = DateTime.UtcNow;
        var rows = data.Clubs.Concat<UserOwnedEntity>(data.Belts).Concat(data.Sessions).Concat(data.Competitions).ToList();
        foreach (var row in rows)
        {
            row.UserId = user.Id;
            row.CreatedAtUtc = now;
        }

        db.AddRange(rows);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
