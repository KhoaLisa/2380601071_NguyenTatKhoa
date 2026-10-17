using Abp.Application.Services;
using NguyenTatKhoa_2380601071.Sessions.Dto;
using System.Threading.Tasks;

namespace NguyenTatKhoa_2380601071.Sessions;

public interface ISessionAppService : IApplicationService
{
    Task<GetCurrentLoginInformationsOutput> GetCurrentLoginInformations();
}
