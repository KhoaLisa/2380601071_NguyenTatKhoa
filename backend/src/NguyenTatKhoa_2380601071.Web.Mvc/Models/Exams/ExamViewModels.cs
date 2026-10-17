using Microsoft.AspNetCore.Http;
using NguyenTatKhoa_2380601071.Toeic.Dto;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using NguyenTatKhoa_2380601071.Toeic;

namespace NguyenTatKhoa_2380601071.Web.Models.Exams;

public class ExamIndexViewModel
{
    public IReadOnlyList<ExamListItemDto> Exams { get; set; } = new List<ExamListItemDto>();
    public ImportExamResultDto ImportResult { get; set; }
    public string Search { get; set; }
    public ExamStatus? Status { get; set; }
    public int TotalCount { get; set; }
}

public class LecturerDashboardViewModel
{
    public IReadOnlyList<ExamListItemDto> Exams { get; set; } = new List<ExamListItemDto>();
}

public class ExamImportViewModel
{
    public ImportExamResultDto Result { get; set; }
    public bool ValidationOnly { get; set; }
    public string FileName { get; set; }
}

public class ExamPublishViewModel
{
    public ExamDetailDto Exam { get; set; }
    public ExamValidationResultDto Validation { get; set; }
}

public class ImportExamFormModel
{
    public IFormFile File { get; set; }
}

public class UpdateExamMetadataFormModel
{
    public Guid Id { get; set; }
    [Required, StringLength(256)]
    public string Title { get; set; }
    [Range(1, 600)]
    public int DurationMinutes { get; set; }
}

public class UpdateContentBlockFormModel
{
    public Guid ExamId { get; set; }
    public Guid BlockId { get; set; }
    public int DisplayOrder { get; set; }
    public string TextContent { get; set; }
}

public class UpdateQuestionFormModel
{
    public Guid ExamId { get; set; }
    public Guid QuestionId { get; set; }
    public string Prompt { get; set; }
    public string Hint { get; set; }
    public bool AllowQuestionShuffle { get; set; }
    public bool AllowOptionShuffle { get; set; }
    public Guid CorrectOptionId { get; set; }
    public List<UpdateQuestionOptionFormModel> Options { get; set; } = new List<UpdateQuestionOptionFormModel>();
}

public class UpdateQuestionOptionFormModel
{
    public Guid Id { get; set; }
    public string Text { get; set; }
}
