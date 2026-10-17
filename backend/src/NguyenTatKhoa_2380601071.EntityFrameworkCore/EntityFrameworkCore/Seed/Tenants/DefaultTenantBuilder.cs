using Abp.MultiTenancy;
using NguyenTatKhoa_2380601071.Editions;
using NguyenTatKhoa_2380601071.MultiTenancy;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace NguyenTatKhoa_2380601071.EntityFrameworkCore.Seed.Tenants;

public class DefaultTenantBuilder
{
    private readonly NguyenTatKhoa_2380601071DbContext _context;

    public DefaultTenantBuilder(NguyenTatKhoa_2380601071DbContext context)
    {
        _context = context;
    }

    public void Create()
    {
        CreateDefaultTenant();
    }

    private void CreateDefaultTenant()
    {
        // Default tenant

        var defaultTenant = _context.Tenants.IgnoreQueryFilters().FirstOrDefault(t => t.TenancyName == AbpTenantBase.DefaultTenantName);
        if (defaultTenant == null)
        {
            defaultTenant = new Tenant(AbpTenantBase.DefaultTenantName, AbpTenantBase.DefaultTenantName);

            var defaultEdition = _context.Editions.IgnoreQueryFilters().FirstOrDefault(e => e.Name == EditionManager.DefaultEditionName);
            if (defaultEdition != null)
            {
                defaultTenant.EditionId = defaultEdition.Id;
            }

            _context.Tenants.Add(defaultTenant);
            _context.SaveChanges();
        }
    }
}
