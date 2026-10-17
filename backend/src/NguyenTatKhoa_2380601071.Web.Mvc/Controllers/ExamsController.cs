using Abp.Application.Services.Dto;
using Abp.AspNetCore.Mvc.Authorization;
using Abp.UI;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using NguyenTatKhoa_2380601071.Authorization;
using NguyenTatKhoa_2380601071.Controllers;
using NguyenTatKhoa_2380601071.Toeic;
using NguyenTatKhoa_2380601071.Toeic.Dto;
using NguyenTatKhoa_2380601071.Web.Models.Exams;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace NguyenTatKhoa_2380601071.Web.Controllers;

[AbpMvcAuthorize(PermissionNames.Pages_Exams)]
public class ExamsController : NguyenTatKhoa_2380601071ControllerBase
{
    private readonly IExamManagementAppService _examService;

    public ExamsController(IExamManagementAppService examService) => _examService = examService;

    public async Task<ActionResult> Index(string search, ExamStatus? status)
    {
        var all = (await _examService.GetAllAsync()).Items;
        var filtered = all.Where(exam => (!status.HasValue || exam.Status == status) &&
            (string.IsNullOrWhiteSpace(search) || exam.Title.Contains(search.Trim(), StringComparison.OrdinalIgnoreCase) ||
                exam.Code.Contains(search.Trim(), StringComparison.OrdinalIgnoreCase))).ToList();
        return View(new ExamIndexViewModel { Exams = filtered, Search = search, Status = status, TotalCount = all.Count });
    }

    public async Task<ActionResult> Details(Guid id) => View(await _examService.GetAsync(new EntityDto<Guid>(id)));

    [AbpMvcAuthorize(PermissionNames.Pages_Exams_Manage)]
    public async Task<ActionResult> Edit(Guid id) => View(await _examService.GetAsync(new EntityDto<Guid>(id)));

    [AbpMvcAuthorize(PermissionNames.Pages_Exams_Statistics)]
    public async Task<ActionResult> Statistics(Guid? id)
    {
        if (id.HasValue) return View(await _examService.GetStatisticsAsync(new EntityDto<Guid>(id.Value)));
        return View("StatisticsIndex", (await _examService.GetAllAsync()).Items);
    }

    [HttpGet]
    [AbpMvcAuthorize(PermissionNames.Pages_Exams_Manage)]
    public ActionResult Import() => View(new ExamImportViewModel());

    [HttpPost]
    [RequestSizeLimit(27 * 1024 * 1024)]
    [AbpMvcAuthorize(PermissionNames.Pages_Exams_Manage)]
    public async Task<ActionResult> Import(ImportExamFormModel model) => await ProcessWordAsync(model, false);

    [HttpPost]
    [RequestSizeLimit(27 * 1024 * 1024)]
    [AbpMvcAuthorize(PermissionNames.Pages_Exams_Manage)]
    public async Task<ActionResult> ValidateWord(ImportExamFormModel model) => await ProcessWordAsync(model, true);

    private async Task<ActionResult> ProcessWordAsync(ImportExamFormModel model, bool validationOnly)
    {
        var viewModel = new ExamImportViewModel { ValidationOnly = validationOnly, FileName = model?.File?.FileName };
        if (model?.File == null || model.File.Length == 0 || model.File.Length > 25 * 1024 * 1024)
        {
            viewModel.Result = new ImportExamResultDto
            {
                Errors = { new ImportValidationErrorDto { Location = "Tệp", Code = "INVALID_FILE", Message = "Chọn tệp Word .docx có nội dung và không vượt quá 25 MB.", Fix = "Kiểm tra tệp rồi tải lại." } }
            };
            return View("Import", viewModel);
        }
        using var stream = new MemoryStream();
        await model.File.CopyToAsync(stream);
        var input = new ImportExamInput { FileName = model.File.FileName, Content = stream.ToArray() };
        viewModel.Result = validationOnly ? await _examService.ValidateWordAsync(input) : await _examService.ImportAsync(input);
        if (!validationOnly && viewModel.Result.Succeeded)
        {
            TempData["SuccessMessage"] = viewModel.Result.AlreadyImported
                ? "Tệp này đã được nhập trước đó. Đang mở đề hiện có."
                : $"Đã nhập đề {viewModel.Result.Code} với {viewModel.Result.QuestionCount} câu hỏi vào bản nháp.";
            return RedirectToAction(nameof(Details), new { id = viewModel.Result.ExamId });
        }
        return View("Import", viewModel);
    }

    [HttpGet]
    [AbpMvcAuthorize(PermissionNames.Pages_Exams_Manage)]
    public async Task<ActionResult> Publish(Guid id) => View(new ExamPublishViewModel
    {
        Exam = await _examService.GetAsync(new EntityDto<Guid>(id)),
        Validation = await _examService.ValidateAsync(new EntityDto<Guid>(id))
    });

    [HttpPost]
    [ActionName("Publish")]
    [AbpMvcAuthorize(PermissionNames.Pages_Exams_Manage)]
    public async Task<ActionResult> ConfirmPublish(Guid id)
    {
        try
        {
            await _examService.PublishAsync(new EntityDto<Guid>(id));
            TempData["SuccessMessage"] = "Đề đã xuất bản và có thể hiển thị trên ứng dụng sinh viên.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (UserFriendlyException exception)
        {
            TempData["ErrorMessage"] = exception.Message;
            return RedirectToAction(nameof(Publish), new { id });
        }
    }

    [HttpPost]
    [AbpMvcAuthorize(PermissionNames.Pages_Exams_Manage)]
    public async Task<ActionResult> Validate(Guid id)
    {
        var validation = await _examService.ValidateAsync(new EntityDto<Guid>(id));
        if (validation.CanPublish) TempData["SuccessMessage"] = "Cấu trúc đề hợp lệ để xuất bản.";
        else TempData["ErrorMessage"] = string.Join("\n", validation.Errors.Select(error => error.Location + ": " + error.Message));
        return RedirectToAction(nameof(Edit), new { id });
    }

    [HttpPost]
    [AbpMvcAuthorize(PermissionNames.Pages_Exams_Manage)]
    public async Task<ActionResult> CopyToDraft(Guid id, string code, string title)
    {
        try
        {
            var copy = await _examService.CopyToDraftAsync(new CopyExamToDraftInput { Id = id, Code = code, Title = title });
            TempData["SuccessMessage"] = "Đã tạo bản nháp mới. Đề gốc và kết quả đã có được giữ nguyên.";
            return RedirectToAction(nameof(Edit), new { id = copy.Id });
        }
        catch (UserFriendlyException exception)
        {
            TempData["ErrorMessage"] = exception.Message;
            return RedirectToAction(nameof(Details), new { id });
        }
    }

    [HttpPost]
    [AbpMvcAuthorize(PermissionNames.Pages_Exams_Manage)]
    public async Task<ActionResult> UpdateMetadata(UpdateExamMetadataFormModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Tên đề bắt buộc, tối đa 256 ký tự. Thời lượng phải từ 1 đến 600 phút.";
            return RedirectToAction(nameof(Edit), new { id = model.Id });
        }
        return await UpdateAsync(new UpdateExamInput { Id = model.Id, Title = model.Title, DurationMinutes = model.DurationMinutes }, "Đã lưu thông tin đề.");
    }

    [HttpPost]
    [AbpMvcAuthorize(PermissionNames.Pages_Exams_Manage)]
    public async Task<ActionResult> UpdateContentBlock(UpdateContentBlockFormModel model)
    {
        var detail = await _examService.GetAsync(new EntityDto<Guid>(model.ExamId));
        return await UpdateAsync(new UpdateExamInput
        {
            Id = model.ExamId, Title = detail.Title, DurationMinutes = detail.DurationMinutes,
            ContentBlocks = { new UpdateContentBlockInput { Id = model.BlockId, DisplayOrder = model.DisplayOrder, TextContent = model.TextContent } }
        }, "Đã lưu khối nội dung và kiểm tra lại cấu trúc.");
    }

    [HttpPost]
    [AbpMvcAuthorize(PermissionNames.Pages_Exams_Manage)]
    public async Task<ActionResult> UpdateQuestion(UpdateQuestionFormModel model)
    {
        var detail = await _examService.GetAsync(new EntityDto<Guid>(model.ExamId));
        return await UpdateAsync(new UpdateExamInput
        {
            Id = model.ExamId, Title = detail.Title, DurationMinutes = detail.DurationMinutes,
            Questions = { new UpdateQuestionInput
            {
                Id = model.QuestionId, Prompt = model.Prompt, Hint = model.Hint,
                AllowQuestionShuffle = model.AllowQuestionShuffle, AllowOptionShuffle = model.AllowOptionShuffle,
                CorrectOptionId = model.CorrectOptionId,
                Options = (model.Options ?? new()).Select(option => new UpdateQuestionOptionInput { Id = option.Id, Text = option.Text }).ToList()
            } }
        }, "Đã lưu câu hỏi và kiểm tra lại cấu trúc.");
    }

    [HttpPost]
    [RequestSizeLimit(11 * 1024 * 1024)]
    [AbpMvcAuthorize(PermissionNames.Pages_Exams_Manage)]
    public async Task<ActionResult> ReplaceImage(Guid examId, Guid blockId, IFormFile file)
    {
        try
        {
            if (file == null || file.Length == 0 || file.Length > ExamImageValidator.MaxBytes)
                throw new UserFriendlyException("Chọn ảnh PNG, JPEG hoặc GIF không vượt quá 10 MB.");
            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            await _examService.ReplaceImageAsync(new ReplaceExamImageInput
            {
                ExamId = examId, BlockId = blockId, FileName = Path.GetFileName(file.FileName), Content = stream.ToArray()
            });
            TempData["SuccessMessage"] = "Đã thay ảnh, giữ nguyên liên kết bài đọc và kiểm tra lại cấu trúc.";
        }
        catch (UserFriendlyException exception) { TempData["ErrorMessage"] = exception.Message; }
        return RedirectToAction(nameof(Edit), new { id = examId });
    }

    private async Task<ActionResult> UpdateAsync(UpdateExamInput input, string success)
    {
        try { await _examService.UpdateAsync(input); TempData["SuccessMessage"] = success; }
        catch (UserFriendlyException exception) { TempData["ErrorMessage"] = exception.Message; }
        return RedirectToAction(nameof(Edit), new { id = input.Id });
    }
}
