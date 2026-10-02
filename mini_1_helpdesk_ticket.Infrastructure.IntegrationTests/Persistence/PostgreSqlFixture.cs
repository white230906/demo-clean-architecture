using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using mini_1_helpdesk_ticket.Infrastructure.Persistence;
using Xunit;

namespace mini_1_helpdesk_ticket.Infrastructure.IntegrationTests.Persistence;

[CollectionDefinition(Name)]
public sealed class PostgreSqlCollection : ICollectionFixture<PostgreSqlFixture>
{
    public const string Name = "PostgreSQL integration tests";
}

public sealed class PostgreSqlFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container =
        new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("helpdesk_tests")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        await using var dbContext = CreateDbContext();
        await dbContext.Database.MigrateAsync();
    }

    public HelpdeskDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<HelpdeskDbContext>()
            .UseNpgsql(_container.GetConnectionString())
            .UseSnakeCaseNamingConvention()
            .Options;

        return new HelpdeskDbContext(options);
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }
}
