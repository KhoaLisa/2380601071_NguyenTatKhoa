using Abp.Application.Services;
using Abp.Application.Services.Dto;
using NguyenTatKhoa_2380601071.Toeic.Dto;
using System.Threading.Tasks;

namespace NguyenTatKhoa_2380601071.Toeic;

public interface IStudentExamAppService : IApplicationService
{
    Task<ListResultDto<PublishedExamDto>> GetPublishedExamsAsync();
    Task<ListResultDto<MyExamAttemptDto>> GetMyAttemptsAsync();
    Task<StartExamOutput> StartAsync(StartExamInput input);
    Task<AttemptDto> GetAttemptAsync(GetAttemptInput input);
    Task<SaveAnswerOutput> SaveAnswerAsync(SaveAnswerInput input);
    Task<AttemptResultDto> SubmitAsync(SubmitAttemptInput input);
}
