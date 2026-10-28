using System.Text.Json;
using MatForge.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace MatForge.Data.Seed;

public class DictionarySeeder(AppDbContext db)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var disciplineSeeds = Read<DisciplineSeed>("disciplines.json");
        var disciplines = await UpsertAsync(db.Disciplines, disciplineSeeds, (_, _) => { }, cancellationToken);

        foreach (var seed in disciplineSeeds)
        {
            var disciplineId = disciplines[seed.Code].Id;

            await UpsertAsync(db.DisciplineFormats.Where(f => f.DisciplineId == disciplineId), seed.Formats,
                (format, _) => format.DisciplineId = disciplineId, cancellationToken);

            await UpsertAsync(db.Belts.Where(b => b.DisciplineId == disciplineId), seed.Belts,
                (belt, beltSeed) =>
                {
                    belt.DisciplineId = disciplineId;
                    belt.Rank = belt.SortOrder;
                    belt.MinDegree = beltSeed.MinDegree;
                    belt.MaxDegree = beltSeed.MaxDegree;
                }, cancellationToken);
        }

        await UpsertAsync(db.TrainingCharacters, Read<TrainingCharacterSeed>("training-characters.json"),
            (character, seed) => character.AllowsRounds = seed.AllowsRounds, cancellationToken);

        await UpsertAsync(db.ActivityTypes, Read<SeedEntry>("activity-types.json"), (_, _) => { }, cancellationToken);

        var categorySeeds = Read<OfferedInDisciplinesSeed>("technique-categories.json");
        var categories = await UpsertAsync(db.TechniqueCategories, categorySeeds, (_, _) => { }, cancellationToken);
        await SyncDisciplineLinksAsync(db.TechniqueCategoryDisciplines,
            categorySeeds.SelectMany(c => c.Disciplines.Select(d => (categories[c.Code].Id, disciplines[d].Id))),
            link => (link.TechniqueCategoryId, link.DisciplineId),
            (categoryId, disciplineId) => new TechniqueCategoryDiscipline { TechniqueCategoryId = categoryId, DisciplineId = disciplineId },
            cancellationToken);

        await UpsertAsync(db.Techniques, Read<TechniqueSeed>("techniques.json"),
            (technique, seed) => technique.CategoryId = categories[seed.Category].Id, cancellationToken);

        var positionSeeds = Read<OfferedInDisciplinesSeed>("positions.json");
        var positions = await UpsertAsync(db.Positions, positionSeeds, (_, _) => { }, cancellationToken);
        await SyncDisciplineLinksAsync(db.PositionDisciplines,
            positionSeeds.SelectMany(p => p.Disciplines.Select(d => (positions[p.Code].Id, disciplines[d].Id))),
            link => (link.PositionId, link.DisciplineId),
            (positionId, disciplineId) => new PositionDiscipline { PositionId = positionId, DisciplineId = disciplineId },
            cancellationToken);

        await UpsertAsync(db.MaterialCategories, Read<SeedEntry>("material-categories.json"), (_, _) => { }, cancellationToken);
    }

    // Adds missing rows and updates existing ones found by code; rows are never deleted.
    // SortOrder follows the order of the entries in the seed file.
    private async Task<Dictionary<string, TEntity>> UpsertAsync<TEntity, TSeed>(
        IQueryable<TEntity> existingRows,
        IReadOnlyList<TSeed> seeds,
        Action<TEntity, TSeed> applyFields,
        CancellationToken cancellationToken)
        where TEntity : DictionaryEntity, new()
        where TSeed : SeedEntry
    {
        var rows = await existingRows.ToDictionaryAsync(e => e.Code, cancellationToken);

        for (var i = 0; i < seeds.Count; i++)
        {
            var seed = seeds[i];
            if (!rows.TryGetValue(seed.Code, out var row))
            {
                row = new TEntity { Code = seed.Code };
                db.Add(row);
                rows[seed.Code] = row;
            }

            row.Name = seed.Name;
            row.SortOrder = i + 1;
            applyFields(row, seed);
        }

        await db.SaveChangesAsync(cancellationToken);
        return rows;
    }

    // Link rows are the only rows the seeder deletes: they must match the seed exactly.
    private async Task SyncDisciplineLinksAsync<TLink>(
        DbSet<TLink> links,
        IEnumerable<(int OwnerId, int DisciplineId)> wantedLinks,
        Func<TLink, (int OwnerId, int DisciplineId)> keyOf,
        Func<int, int, TLink> createLink,
        CancellationToken cancellationToken)
        where TLink : class
    {
        var wanted = wantedLinks.ToHashSet();
        var existing = await links.ToListAsync(cancellationToken);

        links.RemoveRange(existing.Where(link => !wanted.Contains(keyOf(link))));

        var missing = wanted.Except(existing.Select(keyOf));
        links.AddRange(missing.Select(key => createLink(key.OwnerId, key.DisciplineId)));

        await db.SaveChangesAsync(cancellationToken);
    }

    private static List<T> Read<T>(string fileName)
    {
        using var stream = typeof(DictionarySeeder).Assembly.GetManifestResourceStream($"Seed.{fileName}")
            ?? throw new InvalidOperationException($"Seed file '{fileName}' not found.");
        return JsonSerializer.Deserialize<List<T>>(stream, JsonOptions)
            ?? throw new InvalidOperationException($"Seed file '{fileName}' is empty.");
    }

    private class SeedEntry
    {
        public string Code { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
    }

    private class DisciplineSeed : SeedEntry
    {
        public List<SeedEntry> Formats { get; init; } = [];
        public List<BeltSeed> Belts { get; init; } = [];
    }

    private class BeltSeed : SeedEntry
    {
        public int MinDegree { get; init; }
        public int MaxDegree { get; init; }
    }

    private class TrainingCharacterSeed : SeedEntry
    {
        public bool AllowsRounds { get; init; }
    }

    private class OfferedInDisciplinesSeed : SeedEntry
    {
        public List<string> Disciplines { get; init; } = [];
    }

    private class TechniqueSeed : SeedEntry
    {
        public string Category { get; init; } = string.Empty;
    }
}
