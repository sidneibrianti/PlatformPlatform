using PlatformPlatform.SharedKernel.DomainCore.Entities;

namespace PlatformPlatform.AccountManagement.Domain.Tenants;

public sealed class Tenant : AggregateRoot<TenantId>
{
    private Tenant(TenantId id, string name) : base(id)
    {
        Name = name;
        State = TenantState.Trial;
    }
    
    public string Name { get; private set; }
    
    public TenantState State { get; private set; }
    
    public static Tenant Create(TenantId tenantId, string email)
    {
        var tenant = new Tenant(tenantId, tenantId.ToString());
        tenant.AddDomainEvent(new TenantCreatedEvent(tenant.Id, email));
        return tenant;
    }
    
    public void Update(string tenantName)
    {
        Name = tenantName;
    }
}
