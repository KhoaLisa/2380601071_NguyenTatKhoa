using System.Threading.Tasks;

namespace NguyenTatKhoa_2380601071.Toeic;

// SQL Server transaction-owned application lock, shared by all backend processes.
public interface IToeicTransactionLock
{
    Task AcquireAsync(string resource);
}
