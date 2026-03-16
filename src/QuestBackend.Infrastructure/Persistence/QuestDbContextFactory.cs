using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace QuestBackend.Infrastructure.Persistence;

public sealed class QuestDbContextFactory : IDesignTimeDbContextFactory<QuestDbContext>
{
    public QuestDbContext CreateDbContext(string[] args)
    {
        string connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__QuestDatabase")
            ?? "Host=localhost;Port=5432;Database=quest_backend;Username=quest_backend;Password=quest_backend";

        DbContextOptionsBuilder<QuestDbContext> builder = new();
        builder.UseNpgsql(connectionString);
        return new QuestDbContext(builder.Options);
    }
}
