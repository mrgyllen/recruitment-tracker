using System.Data.Common;
using api.Application.Common.Interfaces;
using api.Infrastructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NSubstitute;
using Respawn;
using Testcontainers.MsSql;

namespace api.Application.FunctionalTests;

public class SqlTestcontainersTestDatabase : ITestDatabase
{
    private const string DefaultDatabase = "apiTestDb";
    private readonly MsSqlContainer _container;
    private DbConnection _connection = null!;
    private string _connectionString = null!;
    private Respawner _respawner = null!;

    public SqlTestcontainersTestDatabase()
    {
        _container = new MsSqlBuilder()
            .WithAutoRemove(true)
            .Build();
    }

    public async Task InitialiseAsync()
    {
        await _container.StartAsync();
        await _container.ExecScriptAsync($"CREATE DATABASE {DefaultDatabase}");

        var builder = new SqlConnectionStringBuilder(_container.GetConnectionString())
        {
            InitialCatalog = DefaultDatabase
        };

        _connectionString = builder.ConnectionString;

        _connection = new SqlConnection(_connectionString);

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(_connectionString)
            .ConfigureWarnings(warnings => warnings.Log(RelationalEventId.PendingModelChangesWarning))
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
        await _container.DisposeAsync();
    }
}
