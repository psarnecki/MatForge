using System.Net.Http.Json;
using MatForge.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Testcontainers.MsSql;

namespace MatForge.IntegrationTests;

// Starts one SQL Server container for the whole test run and applies the production migrations
public class DatabaseFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();

    public string ConnectionString { get; private set; } = string.Empty;
    public ApiFactory ApiFactory { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        ConnectionString = new SqlConnectionStringBuilder(_container.GetConnectionString())
        {
            InitialCatalog = "MatForge"
        }.ConnectionString;

        await using var db = CreateDbContext();
        await db.Database.MigrateAsync();

        // Starting the API also runs the dictionary seeder
        ApiFactory = new ApiFactory(ConnectionString);
        _ = ApiFactory.Server;
    }

    public AppDbContext CreateDbContext(Guid? currentUserId = null) =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(ConnectionString).Options,
            new TestCurrentUser(currentUserId));

    // Registers a new user with a unique e-mail and returns a client that keeps the login cookie
    public async Task<LoggedInUser> CreateLoggedInUserAsync()
    {
        var client = ApiFactory.CreateClient();
        var credentials = new { email = $"user-{Guid.NewGuid():N}@test.local", password = "Test1234!" };

        (await client.PostAsJsonAsync("/api/auth/register", credentials)).EnsureSuccessStatusCode();
        (await client.PostAsJsonAsync("/api/auth/login?useCookies=true", credentials)).EnsureSuccessStatusCode();

        var profile = await client.GetFromJsonAsync<ProfileResponse>("/api/me");
        return new LoggedInUser(client, profile!.Id);
    }

    public async Task DisposeAsync()
    {
        await ApiFactory.DisposeAsync();
        await _container.DisposeAsync();
    }

    private record ProfileResponse(Guid Id);
}

public record LoggedInUser(HttpClient Client, Guid Id);

[CollectionDefinition("Database")]
public class DatabaseCollection : ICollectionFixture<DatabaseFixture>;
