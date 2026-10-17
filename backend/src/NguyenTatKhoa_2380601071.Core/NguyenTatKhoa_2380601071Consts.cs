using NguyenTatKhoa_2380601071.Debugging;

namespace NguyenTatKhoa_2380601071;

public class NguyenTatKhoa_2380601071Consts
{
    public const string LocalizationSourceName = "NguyenTatKhoa_2380601071";

    public const string ConnectionStringName = "Default";

    public const bool MultiTenancyEnabled = true;


    /// <summary>
    /// Default pass phrase for SimpleStringCipher decrypt/encrypt operations
    /// </summary>
    public static readonly string DefaultPassPhrase =
        DebugHelper.IsDebug ? "gsKxGZ012HLL3MI5" : "6c157a89f1c442418bae329e7e88af60";
}
