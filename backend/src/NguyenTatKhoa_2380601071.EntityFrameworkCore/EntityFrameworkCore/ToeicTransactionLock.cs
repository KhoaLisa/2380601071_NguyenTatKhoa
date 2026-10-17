using Abp.Dependency;
using Abp.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NguyenTatKhoa_2380601071.Toeic;
using System.Threading.Tasks;

namespace NguyenTatKhoa_2380601071.EntityFrameworkCore;

public class ToeicTransactionLock : IToeicTransactionLock, ITransientDependency
{
    private readonly IDbContextProvider<NguyenTatKhoa_2380601071DbContext> _provider;
    public ToeicTransactionLock(IDbContextProvider<NguyenTatKhoa_2380601071DbContext> provider) => _provider = provider;

    public async Task AcquireAsync(string resource)
    {
        var context = await _provider.GetDbContextAsync();
        await context.Database.ExecuteSqlInterpolatedAsync($@"
DECLARE @result int;
EXEC @result = sys.sp_getapplock @Resource={"TOEIC:" + resource},
    @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=15000;
IF @result < 0 THROW 51001, 'TOEIC transaction lock could not be acquired.', 1;");
    }
}
