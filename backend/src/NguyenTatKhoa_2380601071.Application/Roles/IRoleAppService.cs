using Abp.Application.Services;
using Abp.Application.Services.Dto;
using NguyenTatKhoa_2380601071.Roles.Dto;
using System.Threading.Tasks;

namespace NguyenTatKhoa_2380601071.Roles;

public interface IRoleAppService : IAsyncCrudAppService<RoleDto, int, PagedRoleResultRequestDto, CreateRoleDto, RoleDto>
{
    Task<ListResultDto<PermissionDto>> GetAllPermissions();

    Task<GetRoleForEditOutput> GetRoleForEdit(EntityDto input);

    Task<ListResultDto<RoleListDto>> GetRolesAsync(GetRolesInput input);
}
