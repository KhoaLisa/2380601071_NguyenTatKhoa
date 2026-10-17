using Abp.AspNetCore.Mvc.Controllers;
using Abp.IdentityFramework;
using Microsoft.AspNetCore.Identity;

namespace NguyenTatKhoa_2380601071.Controllers
{
    public abstract class NguyenTatKhoa_2380601071ControllerBase : AbpController
    {
        protected NguyenTatKhoa_2380601071ControllerBase()
        {
            LocalizationSourceName = NguyenTatKhoa_2380601071Consts.LocalizationSourceName;
        }

        protected void CheckErrors(IdentityResult identityResult)
        {
            identityResult.CheckErrors(LocalizationManager);
        }
    }
}
