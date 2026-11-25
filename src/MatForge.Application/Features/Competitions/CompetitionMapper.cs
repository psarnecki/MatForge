using MatForge.Data.Entities;
using Riok.Mapperly.Abstractions;

namespace MatForge.Application.Features.Competitions;

[Mapper]
public static partial class CompetitionMapper
{
    [MapperRequiredMapping(RequiredMappingStrategy.Target)]
    public static partial CompetitionDto ToDto(Competition competition);

    [MapperRequiredMapping(RequiredMappingStrategy.Target)]
    private static partial FightDto ToDto(Fight fight);

    // The service sets the owner and the creation time
    [MapperIgnoreTarget(nameof(Competition.Id))]
    [MapperIgnoreTarget(nameof(Competition.UserId))]
    [MapperIgnoreTarget(nameof(Competition.User))]
    [MapperIgnoreTarget(nameof(Competition.CreatedAtUtc))]
    [MapperIgnoreTarget(nameof(Competition.Discipline))]
    [MapperIgnoreTarget(nameof(Competition.Format))]
    public static partial Competition ToEntity(CompetitionRequest request);

    // The service sets Order from the position in the list
    [MapperIgnoreTarget(nameof(Fight.Id))]
    [MapperIgnoreTarget(nameof(Fight.CompetitionId))]
    [MapperIgnoreTarget(nameof(Fight.Competition))]
    [MapperIgnoreTarget(nameof(Fight.Order))]
    [MapperIgnoreTarget(nameof(Fight.Technique))]
    public static partial Fight ToEntity(FightRequest fight);
}
