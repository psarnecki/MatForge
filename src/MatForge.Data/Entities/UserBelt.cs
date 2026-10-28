using Microsoft.EntityFrameworkCore;

namespace MatForge.Data.Entities;

// One belt per discipline per user
[Index(nameof(UserId), nameof(DisciplineId), IsUnique = true)]
public class UserBelt : UserOwnedEntity
{
    public int DisciplineId { get; set; }
    public Discipline Discipline { get; set; } = null!;
    public int BeltId { get; set; }
    public Belt Belt { get; set; } = null!;
    public int Degree { get; set; }
}
