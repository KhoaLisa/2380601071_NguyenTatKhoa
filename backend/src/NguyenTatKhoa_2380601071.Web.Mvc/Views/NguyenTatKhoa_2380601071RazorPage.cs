using Abp.AspNetCore.Mvc.Views;
using Abp.Runtime.Session;
using Microsoft.AspNetCore.Mvc.Razor.Internal;

namespace NguyenTatKhoa_2380601071.Web.Views;

public abstract class NguyenTatKhoa_2380601071RazorPage<TModel> : AbpRazorPage<TModel>
{
    [RazorInject]
    public IAbpSession AbpSession { get; set; }

    protected NguyenTatKhoa_2380601071RazorPage()
    {
        LocalizationSourceName = NguyenTatKhoa_2380601071Consts.LocalizationSourceName;
    }
}
