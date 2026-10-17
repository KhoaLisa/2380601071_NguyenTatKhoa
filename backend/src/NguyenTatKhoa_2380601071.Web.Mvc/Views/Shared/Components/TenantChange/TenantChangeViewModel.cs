using Abp.AutoMapper;
using NguyenTatKhoa_2380601071.Sessions.Dto;

namespace NguyenTatKhoa_2380601071.Web.Views.Shared.Components.TenantChange;

[AutoMapFrom(typeof(GetCurrentLoginInformationsOutput))]
public class TenantChangeViewModel
{
    public TenantLoginInfoDto Tenant { get; set; }
}
