using Abp.MultiTenancy;
using NguyenTatKhoa_2380601071.Authorization.Users;

namespace NguyenTatKhoa_2380601071.MultiTenancy;

public class Tenant : AbpTenant<User>
{
    public Tenant()
    {
    }

    public Tenant(string tenancyName, string name)
        : base(tenancyName, name)
    {
    }
}
