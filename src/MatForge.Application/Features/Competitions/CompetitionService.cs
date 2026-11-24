using MatForge.Application.Common;
using MatForge.Data;
using MatForge.Data.Entities;
using MatForge.Data.Enums;
using Microsoft.EntityFrameworkCore;

namespace MatForge.Application.Features.Competitions;

public class CompetitionService(AppDbContext db, ICurrentUser currentUser) : ICompetitionService
{
    private Guid UserId => currentUser.UserId ?? throw new InvalidOperationException("No user is logged in.");

    // For GET /api/competitions (newest first, both dates optional)
    public Task<List<CompetitionSummaryDto>> GetAllAsync(CompetitionListQuery query, CancellationToken cancellationToken)
    {
        var competitions = db.Competitions.AsQueryable();
        if (query.From != null)
        {
            competitions = competitions.Where(c => c.Date >= query.From);
        }

        if (query.To != null)
        {
            competitions = competitions.Where(c => c.Date <= query.To);
        }

        return competitions
            .OrderByDescending(c => c.Date).ThenByDescending(c => c.Id)
            .Select(c => new CompetitionSummaryDto(c.Id, c.Name, c.Date, c.Location, c.DisciplineId, c.FormatId,
                c.AgeCategory, c.SkillLevel, c.WeightCategory, c.Place, c.ResultNote,
                c.Fights.Count,
                c.Fights.Count(f => f.Result == FightResult.Win),
                c.Fights.Count(f => f.Result == FightResult.Loss),
                c.Fights.Count(f => f.Result == FightResult.Draw)))
            .ToListAsync(cancellationToken);
    }

    // For GET /api/competitions/{id}
    public async Task<CompetitionDto> GetAsync(int id, CancellationToken cancellationToken)
    {
        var competition = await db.Competitions
            .AsNoTracking()
            .Include(c => c.Fights.OrderBy(f => f.Order))
            .SingleOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new NotFoundException("Competition not found.");

        return CompetitionMapper.ToDto(competition);
    }

    // For POST /api/competitions (rules checked by CompetitionRequestValidator)
    public async Task<CompetitionDto> CreateAsync(CompetitionRequest request, CancellationToken cancellationToken)
    {
        var competition = CompetitionMapper.ToEntity(request);
        competition.UserId = UserId;
        competition.CreatedAtUtc = DateTime.UtcNow;
        NumberFights(competition.Fights);

        db.Competitions.Add(competition);
        await db.SaveChangesAsync(cancellationToken);

        return CompetitionMapper.ToDto(competition);
    }

    // For PUT /api/competitions/{id} (replaces the competition and its fights, rules checked by CompetitionRequestValidator)
    public async Task<CompetitionDto> UpdateAsync(int id, CompetitionRequest request, CancellationToken cancellationToken)
    {
        var competition = await db.Competitions
            .Include(c => c.Fights)
            .SingleOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new NotFoundException("Competition not found.");

        competition.Name = request.Name;
        competition.Date = request.Date!.Value;
        competition.Location = request.Location;
        competition.DisciplineId = request.DisciplineId!.Value;
        competition.FormatId = request.FormatId!.Value;
        competition.AgeCategory = request.AgeCategory;
        competition.SkillLevel = request.SkillLevel;
        competition.WeightCategory = request.WeightCategory;
        competition.Place = request.Place;
        competition.ResultNote = request.ResultNote;
        competition.Notes = request.Notes;
        competition.Fights = request.Fights.Select(CompetitionMapper.ToEntity).ToList();
        NumberFights(competition.Fights);

        await db.SaveChangesAsync(cancellationToken);

        return CompetitionMapper.ToDto(competition);
    }

    // For DELETE /api/competitions/{id} (the database deletes the fights with the competition)
    public async Task DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var competition = await db.Competitions.SingleOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new NotFoundException("Competition not found.");

        if (await db.Goals.AnyAsync(g => g.CompetitionId == id, cancellationToken))
        {
            throw new ConflictException(
                $"\"{competition.Name}\" is used by a goal. Change or delete that goal first.");
        }

        db.Competitions.Remove(competition);
        await db.SaveChangesAsync(cancellationToken);
    }

    private static void NumberFights(List<Fight> fights)
    {
        for (var i = 0; i < fights.Count; i++)
        {
            fights[i].Order = i + 1;
        }
    }
}
