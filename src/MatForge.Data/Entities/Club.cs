using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace MatForge.Data.Entities;

// Club names are unique per user
[Index(nameof(UserId), nameof(Name), IsUnique = true)]
public class Club : UserOwnedEntity
{
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
}
