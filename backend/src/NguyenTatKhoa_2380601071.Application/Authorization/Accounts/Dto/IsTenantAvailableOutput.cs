namespace NguyenTatKhoa_2380601071.Authorization.Accounts.Dto;

public class IsTenantAvailableOutput
{
    public TenantAvailabilityState State { get; set; }

    public int? TenantId { get; set; }

    public IsTenantAvailableOutput()
    {
    }

    public IsTenantAvailableOutput(TenantAvailabilityState state, int? tenantId = null)
    {
        State = state;
        TenantId = tenantId;
    }
}
