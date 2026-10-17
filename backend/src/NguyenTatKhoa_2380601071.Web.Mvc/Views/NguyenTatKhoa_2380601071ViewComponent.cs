using Abp.AspNetCore.Mvc.ViewComponents;

namespace NguyenTatKhoa_2380601071.Web.Views;

public abstract class NguyenTatKhoa_2380601071ViewComponent : AbpViewComponent
{
    protected NguyenTatKhoa_2380601071ViewComponent()
    {
        LocalizationSourceName = NguyenTatKhoa_2380601071Consts.LocalizationSourceName;
    }
}
