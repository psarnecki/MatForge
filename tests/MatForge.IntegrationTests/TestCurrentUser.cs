using MatForge.Data;

namespace MatForge.IntegrationTests;

public class TestCurrentUser(Guid? userId) : ICurrentUser
{
    public Guid? UserId => userId;
}
