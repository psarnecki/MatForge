namespace MatForge.Data.Entities;

public class UserBelt : UserOwnedEntity
{
    public int DisciplineId { get; set; }
    public Discipline Discipline { get; set; } = null!;
    public int BeltId { get; set; }
    public Belt Belt { get; set; } = null!;
    public int Degree { get; set; }
}
