using Abp.MultiTenancy;
using System.ComponentModel.DataAnnotations;

namespace NguyenTatKhoa_2380601071.Authorization.Accounts.Dto;

public class IsTenantAvailableInput
{
    [Required]
    [StringLength(AbpTenantBase.MaxTenancyNameLength)]
    public string TenancyName { get; set; }
}
