using Abp.AspNetCore.Mvc.Authorization;
using NguyenTatKhoa_2380601071.Controllers;
using Microsoft.AspNetCore.Mvc;
using NguyenTatKhoa_2380601071.Authorization;
using NguyenTatKhoa_2380601071.Toeic;
using NguyenTatKhoa_2380601071.Web.Models.Exams;
using System.Threading.Tasks;

namespace NguyenTatKhoa_2380601071.Web.Controllers;

[AbpMvcAuthorize(PermissionNames.Pages_Exams)]
public class HomeController : NguyenTatKhoa_2380601071ControllerBase
{
    private readonly IExamManagementAppService _exams;

    public HomeController(IExamManagementAppService exams)
    {
        _exams = exams;
    }

    public async Task<ActionResult> Index()
    {
        return View(new LecturerDashboardViewModel { Exams = (await _exams.GetAllAsync()).Items });
    }
}
