using System.Data.Common;
using api.Application.Common.Interfaces;
using api.Infrastructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using Respawn;

namespace api.Application.FunctionalTests;

public class SqlTestDatabase : ITestDatabase
{
    private readonly string _connectionString = null!;
    private SqlConnection _connection = null!;
    private Respawner _respawner = null!;

    public SqlTestDatabase()
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("apiDb");

        Guard.Against.Null(connectionString);

        _connectionString = connectionString;
    }

    public async Task InitialiseAsync()
    {
        _connection = new SqlConnection(_connectionString);

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(_connectionString)
            .Options;

        var tenantContext = Substitute.For<ITenantContext>();
        tenantContext.IsServiceContext.Returns(true);
        var context = new ApplicationDbContext(options, tenantContext);

        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();

        _respawner = await Respawner.CreateAsync(_connectionString);
    }

    public DbConnection GetConnection()
    {
        return _connection;
    }

    public string GetConnectionString()
    {
        return _connectionString;
    }

    public async Task ResetAsync()
    {
        await _respawner.ResetAsync(_connectionString);
    }

    public async Task DisposeAsync()
    {
        await _connection.DisposeAsync();
    }
}
