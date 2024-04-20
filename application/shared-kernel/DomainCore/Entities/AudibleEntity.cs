using System.ComponentModel.DataAnnotations;

namespace PlatformPlatform.SharedKernel.DomainCore.Entities;

/// <summary>
///     The AudibleEntity class extends Entity and implements IAuditableEntity, which adds
///     a readonly CreatedAt and private ModifiedAt properties to derived entities.
/// </summary>
public abstract class AudibleEntity<T>(T id) : Entity<T>(id), IAuditableEntity where T : IComparable<T>
{
    public DateTimeOffset CreatedAt { get; init; } = TimeProvider.System.GetUtcNow();
    
    [ConcurrencyCheck]
    public DateTimeOffset? ModifiedAt { get; private set; }
    
    /// <summary>
    ///     This method is used by the UpdateAuditableEntitiesInterceptor in the Infrastructure layer.
    ///     It's not intended to be used by the application, which is why it is implemented using an explicit interface.
    /// </summary>
    void IAuditableEntity.UpdateModifiedAt(DateTimeOffset? modifiedAt)
    {
        ModifiedAt = modifiedAt;
    }
}
