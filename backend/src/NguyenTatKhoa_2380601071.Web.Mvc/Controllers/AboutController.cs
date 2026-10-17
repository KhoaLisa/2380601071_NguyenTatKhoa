using Abp.AspNetCore.Mvc.Authorization;
using NguyenTatKhoa_2380601071.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace NguyenTatKhoa_2380601071.Web.Controllers;

[AbpMvcAuthorize]
public class AboutController : NguyenTatKhoa_2380601071ControllerBase
{
    public ActionResult Index()
    {
        return View();
    }
}
