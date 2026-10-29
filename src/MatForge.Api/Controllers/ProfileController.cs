using MatForge.Application.Features.Profile;
using MatForge.Data.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace MatForge.Api.Controllers;

[ApiController]
[Route("api/me")]
public class ProfileController(IProfileService profileService, SignInManager<AppUser> signInManager) : ControllerBase
{
    [HttpGet]
    public Task<ProfileDto> Get(CancellationToken cancellationToken) =>
        profileService.GetAsync(cancellationToken);

    [HttpPut]
    public Task<ProfileDto> Update(UpdateProfileRequest request, CancellationToken cancellationToken) =>
        profileService.UpdateAsync(request, cancellationToken);

    [HttpDelete]
    public async Task<IActionResult> Delete(CancellationToken cancellationToken)
    {
        await profileService.DeleteAsync(cancellationToken);
        await signInManager.SignOutAsync();
        return NoContent();
    }
}
