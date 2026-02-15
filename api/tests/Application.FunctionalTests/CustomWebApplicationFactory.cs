using System.Data.Common;
using api.Application.Common.Interfaces;
using api.Application.Common.Models;
using api.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;

namespace api.Application.FunctionalTests;

using static Testing;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly DbConnection _connection;
    private readonly string _connectionString;

    public CustomWebApplicationFactory(DbConnection connection, string connectionString)
    {
        _connection = connection;
        _connectionString = connectionString;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder
            .UseEnvironment("Testing")
            .UseSetting("ConnectionStrings:apiDb", _connectionString);

        builder.ConfigureTestServices(services =>
        {
            var user = Substitute.For<IUser>();
            user.Roles.Returns(_ => GetRoles());
            user.Id.Returns(_ => GetUserId());

            services
                .RemoveAll<IUser>()
                .AddTransient(_ => user);

            var blobStorage = Substitute.For<IBlobStorageService>();
            blobStorage.UploadAsync(Arg.Any<string>(), Arg.Any<string>(),
                    Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(ci => ci.ArgAt<string>(1));
            blobStorage.VerifyBlobOwnership(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Guid>())
                .Returns(true);
            blobStorage.GenerateSasUri(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<TimeSpan>())
                .Returns(ci => new Uri($"https://test.blob.core.windows.net/{ci.ArgAt<string>(1)}?sas=token"));
            services.RemoveAll<IBlobStorageService>()
                .AddScoped(_ => blobStorage);

            var pdfSplitter = Substitute.For<IPdfSplitter>();
            pdfSplitter.SplitBundleAsync(Arg.Any<Stream>(),
                    Arg.Any<IProgress<PdfSplitProgress>?>(), Arg.Any<CancellationToken>())
                .Returns(new PdfSplitResult(true, Array.Empty<PdfSplitEntry>(), null));
            services.RemoveAll<IPdfSplitter>()
                .AddScoped(_ => pdfSplitter);

            services
                .RemoveAll<DbContextOptions<ApplicationDbContext>>()
                .AddDbContext<ApplicationDbContext>((sp, options) =>
                {
                    options.AddInterceptors(sp.GetServices<ISaveChangesInterceptor>());
                    options.UseSqlServer(_connection);
                });
        });
    }
}
