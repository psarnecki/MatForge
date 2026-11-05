namespace MatForge.Application.Features.Lookups;

public interface ILookupsService
{
    Task<LookupsDto> GetAsync(CancellationToken cancellationToken);
}
