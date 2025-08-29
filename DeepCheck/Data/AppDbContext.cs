using DeepCheck.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace DeepCheck.Data;

public class AppDbContext : DbContext
{
    public DbSet<TestRun> TestRuns { get; set; }
    public DbSet<TestRunStep> TestRunSteps { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<TestRun>()
          .HasMany(tr => tr.Steps)
          .WithOne(s => s.TestRun)
          .HasForeignKey(s => s.TestRunId)
          .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TestRunStep>()
          .Property(s => s.Status)
          .HasConversion<string>(); // store enum as a string in DB

        modelBuilder.Entity<TestRun>()
          .Property(t => t.RunMethod)
          .HasConversion<string>(); // store enum as a string in DB

        // Override table mappings for all entities to remove schemas
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            modelBuilder.Entity(entityType.ClrType).ToTable(entityType.GetTableName());
        }

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties()
                         .Where(p => p.ClrType == typeof(DateTime) || p.ClrType == typeof(DateTime?)))
            {
                property.SetValueConverter(new ValueConverter<DateTime, DateTime>(
                    v => v,  // store as is
                    v => DateTime.SpecifyKind(v, DateTimeKind.Utc)  // read as UTC
                ));
            }
        }
    }

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }
}
