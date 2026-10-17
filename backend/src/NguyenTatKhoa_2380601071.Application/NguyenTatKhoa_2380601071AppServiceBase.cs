using Abp.Application.Services;
using Abp.IdentityFramework;
using Abp.Runtime.Session;
using NguyenTatKhoa_2380601071.Authorization.Users;
using NguyenTatKhoa_2380601071.MultiTenancy;
using Microsoft.AspNetCore.Identity;
using System;
using System.Threading.Tasks;

namespace NguyenTatKhoa_2380601071;

/// <summary>
/// Derive your application services from this class.
/// </summary>
public abstract class NguyenTatKhoa_2380601071AppServiceBase : ApplicationService
{
    public TenantManager TenantManager { get; set; }

    public UserManager UserManager { get; set; }

    protected NguyenTatKhoa_2380601071AppServiceBase()
    {
        LocalizationSourceName = NguyenTatKhoa_2380601071Consts.LocalizationSourceName;
    }

    protected virtual async Task<User> GetCurrentUserAsync()
    {
        var user = await UserManager.FindByIdAsync(AbpSession.GetUserId().ToString());
        if (user == null)
        {
            throw new Exception("There is no current user!");
        }

        return user;
    }

    protected virtual Task<Tenant> GetCurrentTenantAsync()
    {
        return TenantManager.GetByIdAsync(AbpSession.GetTenantId());
    }

    protected virtual void CheckErrors(IdentityResult identityResult)
    {
        identityResult.CheckErrors(LocalizationManager);
    }
}
