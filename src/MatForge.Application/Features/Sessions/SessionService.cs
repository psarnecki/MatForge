using MatForge.Application.Common;
using MatForge.Data;
using MatForge.Data.Entities;
using MatForge.Data.Enums;
using Microsoft.EntityFrameworkCore;

namespace MatForge.Application.Features.Sessions;

public class SessionService(AppDbContext db, ICurrentUser currentUser) : ISessionService
{
    private Guid UserId => currentUser.UserId ?? throw new InvalidOperationException("No user is logged in.");

    // For GET /api/sessions
    public Task<List<SessionSummaryDto>> GetAllAsync(SessionListQuery query, CancellationToken cancellationToken)
    {
        var sessions = db.TrainingSessions.Where(s => s.Date >= query.From && s.Date <= query.To);
        if (query.Kind != null)
        {
            sessions = sessions.Where(s => s.Kind == query.Kind);
        }

        return sessions
            .OrderBy(s => s.Date).ThenBy(s => s.Id)
            .Select(s => new SessionSummaryDto(s.Id, s.Kind, s.Date, s.DurationMinutes, s.Feeling, s.DisciplineId,
                s.FormatId, s.CharacterId, s.ClubId, s.ActivityTypeId,
                s.Rounds.Count,
                s.Rounds.Count(r => r.Result == RoundResult.Win),
                s.Rounds.Count(r => r.Result == RoundResult.Loss),
                s.Rounds.Count(r => r.Result == RoundResult.Draw)))
            .ToListAsync(cancellationToken);
    }

    // For GET /api/sessions/{id}
    public async Task<SessionDto> GetAsync(int id, CancellationToken cancellationToken)
    {
        var session = await db.TrainingSessions
            .AsNoTracking()
            .Include(s => s.Rounds.OrderBy(r => r.Order))
            .SingleOrDefaultAsync(s => s.Id == id, cancellationToken)
            ?? throw new NotFoundException("Session not found.");

        return SessionMapper.ToDto(session);
    }

    // For POST /api/sessions (rules checked by SessionRequestValidator)
    public async Task<SessionDto> CreateAsync(SessionRequest request, CancellationToken cancellationToken)
    {
        var session = SessionMapper.ToEntity(request);
        session.UserId = UserId;
        session.CreatedAtUtc = DateTime.UtcNow;
        NumberRounds(session.Rounds);

        db.TrainingSessions.Add(session);
        await db.SaveChangesAsync(cancellationToken);

        return SessionMapper.ToDto(session);
    }

    // For PUT /api/sessions/{id} (replaces the session and its rounds, rules checked by SessionRequestValidator)
    public async Task<SessionDto> UpdateAsync(int id, SessionRequest request, CancellationToken cancellationToken)
    {
        var session = await db.TrainingSessions
            .Include(s => s.Rounds)
            .SingleOrDefaultAsync(s => s.Id == id, cancellationToken)
            ?? throw new NotFoundException("Session not found.");

        session.Kind = request.Kind!.Value;
        session.Date = request.Date!.Value;
        session.DurationMinutes = request.DurationMinutes;
        session.Feeling = request.Feeling;
        session.Notes = request.Notes;
        session.DisciplineId = request.DisciplineId;
        session.FormatId = request.FormatId;
        session.CharacterId = request.CharacterId;
        session.ClubId = request.ClubId;
        session.ActivityTypeId = request.ActivityTypeId;
        session.Rounds = request.Rounds.Select(SessionMapper.ToEntity).ToList();
        NumberRounds(session.Rounds);

        await db.SaveChangesAsync(cancellationToken);

        return SessionMapper.ToDto(session);
    }

    // For DELETE /api/sessions/{id} (the database deletes the rounds with the session)
    public async Task DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var session = await db.TrainingSessions.SingleOrDefaultAsync(s => s.Id == id, cancellationToken)
            ?? throw new NotFoundException("Session not found.");

        db.TrainingSessions.Remove(session);
        await db.SaveChangesAsync(cancellationToken);
    }

    private static void NumberRounds(List<SparringRound> rounds)
    {
        for (var i = 0; i < rounds.Count; i++)
        {
            rounds[i].Order = i + 1;
        }
    }
}
