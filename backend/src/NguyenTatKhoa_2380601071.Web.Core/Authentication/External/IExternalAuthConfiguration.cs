using System.Collections.Generic;

namespace NguyenTatKhoa_2380601071.Authentication.External
{
    public interface IExternalAuthConfiguration
    {
        List<ExternalLoginProviderInfo> Providers { get; }
    }
}
