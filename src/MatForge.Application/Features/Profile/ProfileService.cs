using MatForge.Application.Common;
using MatForge.Data;
using Microsoft.EntityFrameworkCore;

namespace MatForge.Application.Features.Profile;

public class ProfileService(AppDbContext db, ICurrentUser currentUser) : IProfileService
{
    private Guid UserId => currentUser.UserId ?? throw new InvalidOperationException("No user is logged in.");

    // For GET /api/me
    public async Task<ProfileDto> GetAsync(CancellationToken cancellationToken)
    {
        var user = await db.Users
            .Where(u => u.Id == UserId)
            .Select(u => new { u.Id, u.Email, u.FirstName, u.LastName })
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("User not found.");

        var clubs = await db.Clubs
            .OrderBy(c => c.Name)
            .Select(c => new ProfileClubDto(c.Id, c.Name))
            .ToListAsync(cancellationToken);

        var belts = await db.UserBelts
            .OrderBy(b => b.Discipline.SortOrder)
            .Select(b => new ProfileBeltDto(b.DisciplineId, b.BeltId, b.Degree))
            .ToListAsync(cancellationToken);

        return new ProfileDto(user.Id, user.Email!, user.FirstName, user.LastName, clubs, belts);
    }

    // For PUT /api/me
    public async Task<ProfileDto> UpdateAsync(UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        var user = await db.Users.SingleOrDefaultAsync(u => u.Id == UserId, cancellationToken)
            ?? throw new NotFoundException("User not found.");

        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        await db.SaveChangesAsync(cancellationToken);

        return await GetAsync(cancellationToken);
    }

    // For DELETE /api/me (one DELETE statement, the database cascades it to all of the user's data)
    public async Task DeleteAsync(CancellationToken cancellationToken)
    {
        await db.Users.Where(u => u.Id == UserId).ExecuteDeleteAsync(cancellationToken);
    }
}
