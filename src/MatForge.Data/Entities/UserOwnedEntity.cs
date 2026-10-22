namespace MatForge.Data.Entities;

public abstract class UserOwnedEntity
{
    public int Id { get; set; }
    public Guid UserId { get; set; }
    public AppUser User { get; set; } = null!;
    public DateTime CreatedAtUtc { get; set; }
}
