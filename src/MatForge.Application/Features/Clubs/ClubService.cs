using MatForge.Application.Common;
using MatForge.Data;
using MatForge.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace MatForge.Application.Features.Clubs;

public class ClubService(AppDbContext db, ICurrentUser currentUser) : IClubService
{
    private Guid UserId => currentUser.UserId ?? throw new InvalidOperationException("No user is logged in.");

    // For GET /api/clubs
    public Task<List<ClubDto>> GetAllAsync(CancellationToken cancellationToken) =>
        db.Clubs
            .OrderBy(c => c.Name)
            .Select(c => new ClubDto(c.Id, c.Name))
            .ToListAsync(cancellationToken);

    // For POST /api/clubs
    public async Task<ClubDto> CreateAsync(ClubRequest request, CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();
        await EnsureNameIsFreeAsync(name, null, cancellationToken);

        var club = new Club { UserId = UserId, Name = name, CreatedAtUtc = DateTime.UtcNow };
        db.Clubs.Add(club);
        await db.SaveChangesAsync(cancellationToken);

        return new ClubDto(club.Id, club.Name);
    }

    // For PUT /api/clubs/{id}
    public async Task<ClubDto> UpdateAsync(int id, ClubRequest request, CancellationToken cancellationToken)
    {
        var club = await FindAsync(id, cancellationToken);
        var name = request.Name.Trim();
        await EnsureNameIsFreeAsync(name, id, cancellationToken);

        club.Name = name;
        await db.SaveChangesAsync(cancellationToken);

        return new ClubDto(club.Id, club.Name);
    }

    // For DELETE /api/clubs/{id}
    public async Task DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var club = await FindAsync(id, cancellationToken);
        if (await db.TrainingSessions.AnyAsync(s => s.ClubId == id, cancellationToken))
        {
            throw new ConflictException(
                $"\"{club.Name}\" is used in your training sessions. Change the club in those sessions first.");
        }

        db.Clubs.Remove(club);
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<Club> FindAsync(int id, CancellationToken cancellationToken) =>
        await db.Clubs.SingleOrDefaultAsync(c => c.Id == id, cancellationToken)
        ?? throw new NotFoundException("Club not found.");

    // Case-insensitive through the database collation
    private async Task EnsureNameIsFreeAsync(string name, int? exceptId, CancellationToken cancellationToken)
    {
        if (await db.Clubs.AnyAsync(c => c.Name == name && c.Id != exceptId, cancellationToken))
        {
            throw new ConflictException($"You already have a club named \"{name}\".");
        }
    }
}
