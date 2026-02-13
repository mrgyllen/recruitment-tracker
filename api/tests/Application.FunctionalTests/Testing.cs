using api.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace api.Application.FunctionalTests;

[SetUpFixture]
public partial class Testing
{
    private static ITestDatabase s_database = null!;
    private static CustomWebApplicationFactory s_factory = null!;
    private static IServiceScopeFactory s_scopeFactory = null!;
    private static string? s_userId;
    private static List<string>? s_roles;
    [OneTimeSetUp]
    public async Task RunBeforeAnyTests()
    {
        s_database = await TestDatabaseFactory.CreateAsync();

        s_factory = new CustomWebApplicationFactory(s_database.GetConnection(), s_database.GetConnectionString());

        s_scopeFactory = s_factory.Services.GetRequiredService<IServiceScopeFactory>();
    }

    public static async Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request)
    {
        using var scope = s_scopeFactory.CreateScope();

        var mediator = scope.ServiceProvider.GetRequiredService<ISender>();

        return await mediator.Send(request);
    }

    public static async Task SendAsync(IBaseRequest request)
    {
        using var scope = s_scopeFactory.CreateScope();

        var mediator = scope.ServiceProvider.GetRequiredService<ISender>();

        await mediator.Send(request);
    }

    public static string? GetUserId()
    {
        return s_userId;
    }

    public static List<string>? GetRoles()
    {
        return s_roles;
    }

    public static Task<string> RunAsDefaultUserAsync()
    {
        return RunAsUserAsync("test@local", Array.Empty<string>());
    }

    public static Task<string> RunAsAdministratorAsync()
    {
        return RunAsUserAsync("administrator@local", new[] { "Administrator" });
    }

    public static Task<string> RunAsUserAsync(string userName, string[] roles)
    {
        // With Entra ID authentication, user management is external
        // For testing purposes, we simulate a user by setting test user ID and roles
        // The actual authentication would be handled by Entra ID in production
        s_userId = Guid.NewGuid().ToString();
        s_roles = roles.ToList();
        return Task.FromResult(s_userId);
    }

    public static async Task ResetState()
    {
        try
        {
            await s_database.ResetAsync();
        }
        catch (Exception)
        {
        }

        s_userId = null;
    }

    public static async Task<TEntity?> FindAsync<TEntity>(params object[] keyValues)
        where TEntity : class
    {
        using var scope = s_scopeFactory.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        return await context.FindAsync<TEntity>(keyValues);
    }

    public static async Task AddAsync<TEntity>(TEntity entity)
        where TEntity : class
    {
        using var scope = s_scopeFactory.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        context.Add(entity);

        await context.SaveChangesAsync();
    }

    public static async Task<int> CountAsync<TEntity>() where TEntity : class
    {
        using var scope = s_scopeFactory.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        return await context.Set<TEntity>().CountAsync();
    }

    [OneTimeTearDown]
    public async Task RunAfterAnyTests()
    {
        await s_database.DisposeAsync();
        await s_factory.DisposeAsync();
    }
}
