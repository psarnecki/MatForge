namespace MatForge.Application.Features.Lookups;

public record LookupsDto(
    List<DisciplineDto> Disciplines,
    List<TrainingCharacterDto> Characters,
    List<LookupItemDto> ActivityTypes,
    List<TechniqueCategoryDto> TechniqueCategories,
    List<TechniqueDto> Techniques,
    List<PositionDto> Positions,
    List<LookupItemDto> MaterialCategories);

public record LookupItemDto(int Id, string Code, string Name);

public record DisciplineDto(int Id, string Code, string Name, List<LookupItemDto> Formats, List<BeltDto> Belts);

public record BeltDto(int Id, string Code, string Name, int Rank, int MinDegree, int MaxDegree);

public record TrainingCharacterDto(int Id, string Code, string Name, bool AllowsRounds);

public record TechniqueCategoryDto(int Id, string Code, string Name, List<int> DisciplineIds);

public record TechniqueDto(int Id, string Code, string Name, int CategoryId);

public record PositionDto(int Id, string Code, string Name, List<int> DisciplineIds);
