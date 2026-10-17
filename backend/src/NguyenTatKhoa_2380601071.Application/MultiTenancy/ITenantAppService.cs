using Abp.Application.Services;
using NguyenTatKhoa_2380601071.MultiTenancy.Dto;

namespace NguyenTatKhoa_2380601071.MultiTenancy;

public interface ITenantAppService : IAsyncCrudAppService<TenantDto, int, PagedTenantResultRequestDto, CreateTenantDto, TenantDto>
{
}

