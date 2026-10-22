using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace MatForge.Data.Entities;

public class AppUser : IdentityUser<Guid>
{
    [MaxLength(100)]
    public string? FirstName { get; set; }

    [MaxLength(100)]
    public string? LastName { get; set; }
}
