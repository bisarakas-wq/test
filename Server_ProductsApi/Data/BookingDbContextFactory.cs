using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Server_ProductsApi.Data;

public sealed class BookingDbContextFactory : IDesignTimeDbContextFactory<BookingDbContext>
{
    public BookingDbContext CreateDbContext(string[] args)
    {
        var projectDirectory = FindProjectDirectory();
        var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
            ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            ?? "Production";
        var configuration = new ConfigurationBuilder()
            .SetBasePath(projectDirectory)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();
        var databaseSetting = configuration["Database:Path"] ?? "App_Data/bookings.db";
        var databasePath = Path.GetFullPath(databaseSetting, projectDirectory);
        var options = new DbContextOptionsBuilder<BookingDbContext>()
            .UseSqlite($"Data Source={databasePath}")
            .Options;
        return new BookingDbContext(options);
    }

    private static string FindProjectDirectory()
    {
        foreach (var startingPath in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            for (var directory = new DirectoryInfo(startingPath); directory is not null; directory = directory.Parent)
            {
                if (File.Exists(Path.Combine(directory.FullName, "Server_ProductsApi.csproj")))
                {
                    return directory.FullName;
                }
            }
        }

        throw new InvalidOperationException("Could not find Server_ProductsApi.csproj to locate the SQLite database.");
    }
}
