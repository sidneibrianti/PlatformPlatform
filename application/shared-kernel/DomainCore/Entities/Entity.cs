using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using PlatformPlatform.SharedKernel.DomainCore.Identity;

namespace PlatformPlatform.SharedKernel.DomainCore.Entities;

/// <summary>
///     The Entity class is a base class for entities which represents business objects.
///     Entities are a DDD concept, where an entity is a business object that has a unique identity.
///     If two entities have the same identity, they are considered to be the same entity.
///     It is recommended to use a <see cref="StronglyTypedLongId{T}" /> for the ID to make the domain more meaningful.
/// </summary>
public abstract class Entity<T>(T id) : IEquatable<Entity<T>> where T : IComparable<T>
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public T Id { get; init; } = id;
    
    public virtual bool Equals(Entity<T>? other)
    {
        return !ReferenceEquals(null, other)
               && (ReferenceEquals(this, other) || EqualityComparer<T>.Default.Equals(Id, other.Id));
    }
    
    public override bool Equals(object? obj)
    {
        return Equals(obj as Entity<T>);
    }
    
    public override int GetHashCode()
    {
        return EqualityComparer<T>.Default.GetHashCode(Id);
    }
    
    public static bool operator ==(Entity<T>? a, Entity<T>? b)
    {
        return a?.Equals(b) == true;
    }
    
    public static bool operator !=(Entity<T>? a, Entity<T>? b)
    {
        return !(a == b);
    }
}
