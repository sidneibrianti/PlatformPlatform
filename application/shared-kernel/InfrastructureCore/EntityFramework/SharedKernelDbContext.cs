using Microsoft.EntityFrameworkCore;
using PlatformPlatform.SharedKernel.DomainCore.Entities;

namespace PlatformPlatform.SharedKernel.InfrastructureCore.EntityFramework;

/// <summary>
///     The SharedKernelDbContext class represents the Entity Framework Core DbContext for managing data access to the
///     database, like creation, querying, and updating of <see cref="IAggregateRoot" /> entities.
/// </summary>
public abstract class SharedKernelDbContext<TContext>(DbContextOptions<TContext> options)
    : DbContext(options) where TContext : DbContext
{
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
        optionsBuilder.AddInterceptors(new UpdateAuditableEntitiesInterceptor());
        
        base.OnConfiguring(optionsBuilder);
    }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Ensures that all enum properties are stored as strings in the database.
        modelBuilder.UseStringForEnums();
        
        base.OnModelCreating(modelBuilder);
    }
}
