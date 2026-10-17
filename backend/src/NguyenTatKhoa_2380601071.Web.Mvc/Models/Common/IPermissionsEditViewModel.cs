using NguyenTatKhoa_2380601071.Roles.Dto;
using System.Collections.Generic;

namespace NguyenTatKhoa_2380601071.Web.Models.Common;

public interface IPermissionsEditViewModel
{
    List<FlatPermissionDto> Permissions { get; set; }
}