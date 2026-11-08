using MatForge.Data;
using Microsoft.EntityFrameworkCore;

namespace MatForge.Application.Common;

// Dictionary checks shared by sessions and competitions
public static class DisciplineRules
{
    public static Task<bool> FormatBelongsToDisciplineAsync(AppDbContext db, int formatId, int disciplineId,
        CancellationToken cancellationToken) =>
        db.DisciplineFormats.AnyAsync(f => f.Id == formatId && f.DisciplineId == disciplineId, cancellationToken);

    public static Task<HashSet<int>> GetOfferedTechniqueIdsAsync(AppDbContext db, List<int> techniqueIds,
        int disciplineId, CancellationToken cancellationToken) =>
        db.Techniques
            .Where(t => techniqueIds.Contains(t.Id) && t.Category.Disciplines.Any(d => d.DisciplineId == disciplineId))
            .Select(t => t.Id)
            .ToHashSetAsync(cancellationToken);
}
