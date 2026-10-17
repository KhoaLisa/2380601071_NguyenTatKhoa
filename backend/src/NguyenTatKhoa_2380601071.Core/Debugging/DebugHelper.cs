namespace NguyenTatKhoa_2380601071.Debugging;

public static class DebugHelper
{
    public static bool IsDebug
    {
        get
        {
#pragma warning disable
#if DEBUG
            return true;
#endif
            return false;
#pragma warning restore
        }
    }
}
