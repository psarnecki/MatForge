using Microsoft.EntityFrameworkCore;

namespace MatForge.Data.Seed;

// Ids of the dictionary rows, found by their codes because ids differ between databases
public record DemoDictionary(
    Dictionary<string, int> Disciplines,
    Dictionary<(string Discipline, string Format), int> Formats,
    Dictionary<(string Discipline, string Belt), int> Belts,
    Dictionary<string, int> Characters,
    Dictionary<string, int> ActivityTypes,
    Dictionary<string, int> Techniques,
    Dictionary<string, int> Positions)
{
    public static async Task<DemoDictionary> LoadAsync(AppDbContext db, CancellationToken cancellationToken = default) =>
        new(
            await db.Disciplines.Select(d => new { d.Code, d.Id })
                .ToDictionaryAsync(d => d.Code, d => d.Id, cancellationToken),
            await db.DisciplineFormats.Select(f => new { Discipline = f.Discipline.Code, f.Code, f.Id })
                .ToDictionaryAsync(f => (f.Discipline, f.Code), f => f.Id, cancellationToken),
            await db.Belts.Select(b => new { Discipline = b.Discipline.Code, b.Code, b.Id })
                .ToDictionaryAsync(b => (b.Discipline, b.Code), b => b.Id, cancellationToken),
            await db.TrainingCharacters.Select(c => new { c.Code, c.Id })
                .ToDictionaryAsync(c => c.Code, c => c.Id, cancellationToken),
            await db.ActivityTypes.Select(a => new { a.Code, a.Id })
                .ToDictionaryAsync(a => a.Code, a => a.Id, cancellationToken),
            await db.Techniques.Select(t => new { t.Code, t.Id })
                .ToDictionaryAsync(t => t.Code, t => t.Id, cancellationToken),
            await db.Positions.Select(p => new { p.Code, p.Id })
                .ToDictionaryAsync(p => p.Code, p => p.Id, cancellationToken));
}
