using Abp.AutoMapper;
using NguyenTatKhoa_2380601071.Roles.Dto;
using NguyenTatKhoa_2380601071.Web.Models.Common;

namespace NguyenTatKhoa_2380601071.Web.Models.Roles;

[AutoMapFrom(typeof(GetRoleForEditOutput))]
public class EditRoleModalViewModel : GetRoleForEditOutput, IPermissionsEditViewModel
{
    public bool HasPermission(FlatPermissionDto permission)
    {
        return GrantedPermissionNames.Contains(permission.Name);
    }
}
