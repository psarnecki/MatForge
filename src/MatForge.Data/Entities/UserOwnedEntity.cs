namespace MatForge.Data.Entities;

public abstract class UserOwnedEntity
{
    public int Id { get; set; }
    public Guid UserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
