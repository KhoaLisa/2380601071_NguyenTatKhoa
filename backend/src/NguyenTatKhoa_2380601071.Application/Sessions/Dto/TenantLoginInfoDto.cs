using Abp.Application.Services.Dto;
using Abp.AutoMapper;
using NguyenTatKhoa_2380601071.MultiTenancy;

namespace NguyenTatKhoa_2380601071.Sessions.Dto;

[AutoMapFrom(typeof(Tenant))]
public class TenantLoginInfoDto : EntityDto
{
    public string TenancyName { get; set; }

    public string Name { get; set; }
}
