using Abp.Application.Services.Dto;
using Abp.AspNetCore.Mvc.Authorization;
using NguyenTatKhoa_2380601071.Authorization;
using NguyenTatKhoa_2380601071.Controllers;
using NguyenTatKhoa_2380601071.MultiTenancy;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace NguyenTatKhoa_2380601071.Web.Controllers;

[AbpMvcAuthorize(PermissionNames.Pages_Tenants)]
public class TenantsController : NguyenTatKhoa_2380601071ControllerBase
{
    private readonly ITenantAppService _tenantAppService;

    public TenantsController(ITenantAppService tenantAppService)
    {
        _tenantAppService = tenantAppService;
    }

    public ActionResult Index() => View();

    public async Task<ActionResult> EditModal(int tenantId)
    {
        var tenantDto = await _tenantAppService.GetAsync(new EntityDto(tenantId));
        return PartialView("_EditModal", tenantDto);
    }
}
