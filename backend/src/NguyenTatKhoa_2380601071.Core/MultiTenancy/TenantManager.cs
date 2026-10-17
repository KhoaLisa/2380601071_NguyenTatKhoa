using Abp.Application.Features;
using Abp.Domain.Repositories;
using Abp.MultiTenancy;
using NguyenTatKhoa_2380601071.Authorization.Users;
using NguyenTatKhoa_2380601071.Editions;

namespace NguyenTatKhoa_2380601071.MultiTenancy;

public class TenantManager : AbpTenantManager<Tenant, User>
{
    public TenantManager(
        IRepository<Tenant> tenantRepository,
        IRepository<TenantFeatureSetting, long> tenantFeatureRepository,
        EditionManager editionManager,
        IAbpZeroFeatureValueStore featureValueStore)
        : base(
            tenantRepository,
            tenantFeatureRepository,
            editionManager,
            featureValueStore)
    {
    }
}
