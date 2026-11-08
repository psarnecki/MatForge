using MatForge.Data.Entities;
using Riok.Mapperly.Abstractions;

namespace MatForge.Application.Features.Sessions;

[Mapper]
public static partial class SessionMapper
{
    [MapperRequiredMapping(RequiredMappingStrategy.Target)]
    public static partial SessionDto ToDto(TrainingSession session);

    [MapperRequiredMapping(RequiredMappingStrategy.Target)]
    private static partial RoundDto ToDto(SparringRound round);

    // The service sets the owner and the creation time
    [MapperIgnoreTarget(nameof(TrainingSession.Id))]
    [MapperIgnoreTarget(nameof(TrainingSession.UserId))]
    [MapperIgnoreTarget(nameof(TrainingSession.User))]
    [MapperIgnoreTarget(nameof(TrainingSession.CreatedAtUtc))]
    [MapperIgnoreTarget(nameof(TrainingSession.Discipline))]
    [MapperIgnoreTarget(nameof(TrainingSession.Format))]
    [MapperIgnoreTarget(nameof(TrainingSession.Character))]
    [MapperIgnoreTarget(nameof(TrainingSession.Club))]
    [MapperIgnoreTarget(nameof(TrainingSession.ActivityType))]
    public static partial TrainingSession ToEntity(SessionRequest request);

    // The service sets Order from the position in the list
    [MapperIgnoreTarget(nameof(SparringRound.Id))]
    [MapperIgnoreTarget(nameof(SparringRound.TrainingSessionId))]
    [MapperIgnoreTarget(nameof(SparringRound.TrainingSession))]
    [MapperIgnoreTarget(nameof(SparringRound.Order))]
    [MapperIgnoreTarget(nameof(SparringRound.Technique))]
    [MapperIgnoreTarget(nameof(SparringRound.Position))]
    public static partial SparringRound ToEntity(RoundRequest round);
}
