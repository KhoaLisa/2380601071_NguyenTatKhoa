using Abp.Application.Services;
using Abp.Application.Services.Dto;
using NguyenTatKhoa_2380601071.Toeic.Dto;
using System;
using System.Threading.Tasks;

namespace NguyenTatKhoa_2380601071.Toeic;

public interface IExamManagementAppService : IApplicationService
{
    Task<ListResultDto<ExamListItemDto>> GetAllAsync();
    Task<ExamDetailDto> GetAsync(EntityDto<Guid> input);
    Task<ImportExamResultDto> ImportAsync(ImportExamInput input);
    Task<ImportExamResultDto> ValidateWordAsync(ImportExamInput input);
    Task<ExamValidationResultDto> ValidateAsync(EntityDto<Guid> input);
    Task<ExamDetailDto> CopyToDraftAsync(CopyExamToDraftInput input);
    Task UpdateAsync(UpdateExamInput input);
    Task ReplaceImageAsync(ReplaceExamImageInput input);
    Task PublishAsync(EntityDto<Guid> input);
    Task<ExamStatisticsDto> GetStatisticsAsync(EntityDto<Guid> input);
}
