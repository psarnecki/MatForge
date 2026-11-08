using MatForge.Application.Common;
using MatForge.Application.Features.Clubs;
using MatForge.Data;
using MatForge.Data.Entities;
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
            .Select(c => new ClubDto(c.Id, c.Name))
            .ToListAsync(cancellationToken);

        var belts = await GetBeltsAsync(cancellationToken);

        return new ProfileDto(user.Id, user.Email!, user.FirstName, user.LastName, clubs, belts);
    }

    // For PUT /api/me
    public async Task<ProfileDto> UpdateAsync(UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        var user = await db.Users.SingleOrDefaultAsync(u => u.Id == UserId, cancellationToken)
            ?? throw new NotFoundException("User not found.");

        user.FirstName = request.FirstName.Trim();
        user.LastName = string.IsNullOrWhiteSpace(request.LastName) ? null : request.LastName.Trim();
        await db.SaveChangesAsync(cancellationToken);

        return await GetAsync(cancellationToken);
    }

    // For PUT /api/me/belts (replaces the whole list, rules checked by UpdateBeltsRequestValidator)
    public async Task<List<ProfileBeltDto>> UpdateBeltsAsync(UpdateBeltsRequest request,
        CancellationToken cancellationToken)
    {
        var existing = await db.UserBelts.ToListAsync(cancellationToken);
        db.UserBelts.RemoveRange(existing.Where(e => request.Belts.All(b => b.DisciplineId != e.DisciplineId)));

        foreach (var item in request.Belts)
        {
            var userBelt = existing.SingleOrDefault(e => e.DisciplineId == item.DisciplineId);
            if (userBelt == null)
            {
                userBelt = new UserBelt { UserId = UserId, DisciplineId = item.DisciplineId, CreatedAtUtc = DateTime.UtcNow };
                db.UserBelts.Add(userBelt);
            }

            userBelt.BeltId = item.BeltId;
            userBelt.Degree = item.Degree;
        }

        await db.SaveChangesAsync(cancellationToken);
        return await GetBeltsAsync(cancellationToken);
    }

    // For DELETE /api/me (one DELETE statement, the database cascades it to all of the user's data)
    public async Task DeleteAsync(CancellationToken cancellationToken)
    {
        await db.Users.Where(u => u.Id == UserId).ExecuteDeleteAsync(cancellationToken);
    }

    private Task<List<ProfileBeltDto>> GetBeltsAsync(CancellationToken cancellationToken) =>
        db.UserBelts
            .OrderBy(b => b.Discipline.SortOrder)
            .Select(b => new ProfileBeltDto(b.DisciplineId, b.BeltId, b.Degree))
            .ToListAsync(cancellationToken);
}
