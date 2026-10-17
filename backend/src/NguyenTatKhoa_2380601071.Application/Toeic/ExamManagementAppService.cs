using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.UI;
using Microsoft.EntityFrameworkCore;
using NguyenTatKhoa_2380601071.Authorization;
using NguyenTatKhoa_2380601071.Toeic.Dto;
using NguyenTatKhoa_2380601071.Toeic.Importing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Text.Json;

namespace NguyenTatKhoa_2380601071.Toeic;

[AbpAuthorize(PermissionNames.Pages_Exams)]
[UnitOfWork(true)]
public class ExamManagementAppService : NguyenTatKhoa_2380601071AppServiceBase, IExamManagementAppService
{
    private const int MaxUploadBytes = 25 * 1024 * 1024;

    private readonly IRepository<Exam, Guid> _examRepository;
    private readonly IRepository<Question, Guid> _questionRepository;
    private readonly IRepository<QuestionOption, Guid> _optionRepository;
    private readonly IRepository<ContentBlock, Guid> _contentBlockRepository;
    private readonly IRepository<ExamAttempt, Guid> _attemptRepository;
    private readonly IRepository<AttemptQuestion, Guid> _attemptQuestionRepository;
    private readonly IDocxExamParser _parser;
    private readonly IToeicTransactionLock _transactionLock;

    public ExamManagementAppService(
        IRepository<Exam, Guid> examRepository,
        IRepository<Question, Guid> questionRepository,
        IRepository<QuestionOption, Guid> optionRepository,
        IRepository<ContentBlock, Guid> contentBlockRepository,
        IRepository<ExamAttempt, Guid> attemptRepository,
        IRepository<AttemptQuestion, Guid> attemptQuestionRepository,
        IDocxExamParser parser, IToeicTransactionLock transactionLock)
    {
        _examRepository = examRepository;
        _questionRepository = questionRepository;
        _optionRepository = optionRepository;
        _contentBlockRepository = contentBlockRepository;
        _attemptRepository = attemptRepository;
        _attemptQuestionRepository = attemptQuestionRepository;
        _parser = parser;
        _transactionLock = transactionLock;
    }

    public async Task<ListResultDto<ExamListItemDto>> GetAllAsync()
    {
        EnsureHostScope();
        var items = await _examRepository.GetAll()
            .OrderByDescending(x => x.CreationTime)
            .Select(x => new ExamListItemDto
            {
                Id = x.Id,
                Code = x.Code,
                Title = x.Title,
                DurationMinutes = x.DurationMinutes,
                Status = x.Status,
                Version = x.Version,
                IsLocked = x.IsLocked,
                IsOfficialFormat = x.IsOfficialFormat,
                PublishedAtUtc = x.PublishedAtUtc,
                QuestionCount = x.Questions.Count,
                AttemptCount = x.Attempts.Count,
                CompletedAttemptCount = x.Attempts.Count(a => a.Status != AttemptStatus.InProgress)
            })
            .ToListAsync();

        return new ListResultDto<ExamListItemDto>(items);
    }

    public async Task<ExamDetailDto> GetAsync(EntityDto<Guid> input)
    {
        EnsureHostScope();
        var exam = await _examRepository.GetAll()
            .AsNoTracking()
            .AsSplitQuery()
            .Include(x => x.Parts)
            .Include(x => x.PassageGroups).ThenInclude(x => x.Passages)
            .Include(x => x.Questions).ThenInclude(x => x.Options)
            .FirstOrDefaultAsync(x => x.Id == input.Id);
        if (exam == null)
        {
            throw new UserFriendlyException("Không tìm thấy đề thi.");
        }

        var detail = MapDetail(exam);
        var blocks = await _contentBlockRepository.GetAll().AsNoTracking()
            .Where(x => x.PassageGroup.ExamId == exam.Id)
            .Select(x => new
            {
                x.PassageGroupId,
                Block = new ContentBlockManagementDto
                {
                    Id = x.Id, PassageId = x.PassageId, SourceBlockId = x.SourceBlockId, SourcePage = x.SourcePage,
                    DocumentKey = x.DocumentKey, DisplayOrder = x.DisplayOrder, Type = x.Type, TextContent = x.TextContent,
                    MediaType = x.MediaType, FileName = x.FileName
                }
            }).ToListAsync();
        foreach (var group in detail.Groups)
        {
            group.ContentBlocks = blocks.Where(x => x.PassageGroupId == group.Id).Select(x => x.Block).OrderBy(x => x.DisplayOrder).ToList();
            foreach (var block in group.ContentBlocks.Where(x => x.Type == ContentBlockType.Image)) block.ContentUrl = "/api/toeic/content/" + block.Id;
        }
        var counts = await _attemptRepository.GetAll().Where(x => x.ExamId == exam.Id).GroupBy(x => x.ExamId)
            .Select(x => new { Total = x.Count(), Completed = x.Count(a => a.Status != AttemptStatus.InProgress) }).FirstOrDefaultAsync();
        detail.AttemptCount = counts?.Total ?? 0;
        detail.CompletedAttemptCount = counts?.Completed ?? 0;
        return detail;
    }

    [AbpAuthorize(PermissionNames.Pages_Exams_Manage)]
    [UnitOfWork(true)]
    public async Task<ImportExamResultDto> ImportAsync(ImportExamInput input)
    {
        EnsureHostScope();
        var result = ParseUpload(input, out var parsed);
        if (parsed?.IsValid != true) return result;

        var hash = Convert.ToHexString(SHA256.HashData(input.Content));
        await _transactionLock.AcquireAsync("import:" + hash);
        await _transactionLock.AcquireAsync("code:" + parsed.Code.Trim().ToUpperInvariant());
        var previous = await _examRepository.GetAll().Where(x => x.SourceHash == hash).OrderBy(x => x.CreationTime).FirstOrDefaultAsync();
        if (previous != null)
        {
            result.Succeeded = true;
            result.ExamId = previous.Id;
            result.AlreadyImported = true;
            return result;
        }
        if (await CodeExistsAsync(parsed.Code))
        {
            result.Errors.Add(Error("EXAM", "DUPLICATE_EXAM_CODE", $"Mã đề {parsed.Code} đã tồn tại.", "Đổi ID/code của đề hoặc chỉnh sửa đề hiện có."));
            return result;
        }

        var exam = new Exam
        {
            Id = Guid.NewGuid(),
            Code = parsed.Code.Trim(),
            Title = parsed.Title.Trim(),
            SourceExamId = parsed.SourceExamId ?? parsed.Code.Trim(),
            TagVersion = parsed.SpecificationVersion,
            Directions = parsed.Directions,
            DeclaredQuestionCount = parsed.DeclaredQuestionCount > 0 ? parsed.DeclaredQuestionCount : parsed.Questions.Count,
            SourceFileName = Path.GetFileName(input.FileName),
            SourceHash = hash,
            DurationMinutes = parsed.DurationMinutes,
            IsOfficialFormat = parsed.IsOfficial,
            Status = ExamStatus.Draft,
            Version = 1
        };

        var parsedParts = parsed.Parts.Count > 0 ? parsed.Parts : new[] { ToeicPart.Part5, ToeicPart.Part6, ToeicPart.Part7 }
            .Select(part => new ParsedExamPart { Number = part, DisplayOrder = (int)part - 4 }).ToList();
        foreach (var part in parsedParts)
        {
            exam.Parts.Add(new ExamPart { Id = Guid.NewGuid(), ExamId = exam.Id, PartNumber = part.Number,
                DisplayOrder = part.DisplayOrder, Directions = part.Directions, AllowShuffleGroups = part.AllowShuffleGroups });
        }

        var groups = new Dictionary<string, PassageGroup>(StringComparer.Ordinal);
        foreach (var parsedGroup in parsed.Groups.OrderBy(x => x.DisplayOrder))
        {
            var group = new PassageGroup
            {
                Id = Guid.NewGuid(),
                ExamId = exam.Id,
                ExamPartId = exam.Parts.Single(x => x.PartNumber == parsedGroup.Part).Id,
                FirstQuestionNumber = parsed.Questions.Where(x => x.GroupExternalId == parsedGroup.ExternalId).Min(x => x.OriginalNumber),
                LastQuestionNumber = parsed.Questions.Where(x => x.GroupExternalId == parsedGroup.ExternalId).Max(x => x.OriginalNumber),
                ExternalId = parsedGroup.ExternalId,
                Title = parsedGroup.Title,
                Part = parsedGroup.Part,
                DisplayOrder = parsedGroup.DisplayOrder,
                AllowQuestionShuffle = parsedGroup.AllowQuestionShuffle
            };
            foreach (var document in parsedGroup.ContentBlocks.GroupBy(x => x.DocumentKey))
            {
                var metadata = parsedGroup.Passages.SingleOrDefault(x => x.Id == document.Key);
                var passage = new Passage
                {
                    Id = Guid.NewGuid(), PassageGroupId = group.Id, SourcePassageId = document.Key,
                    Kind = metadata?.Kind ?? "document", DisplayOrder = metadata?.DisplayOrder ?? group.Passages.Count + 1,
                    BlankQuestionNumbers = JsonSerializer.Serialize(metadata?.BlankQuestionNumbers.ToArray() ?? document.Where(x => x.Type != ContentBlockType.Image)
                        .SelectMany(x => Regex.Matches(x.TextContent ?? "", @"\{\{BLANK:(\d{3})\}\}")
                            .Cast<Match>().Select(m => int.Parse(m.Groups[1].Value))).ToArray())
                };
                group.Passages.Add(passage);
                foreach (var parsedBlock in document.OrderBy(x => x.DisplayOrder))
                {
                    group.ContentBlocks.Add(new ContentBlock
                    {
                        Id = Guid.NewGuid(), PassageGroupId = group.Id, PassageId = passage.Id,
                        SourceBlockId = parsedBlock.SourceBlockId ?? "block-" + parsedBlock.DisplayOrder,
                        SourcePage = parsedBlock.SourcePage,
                        DocumentKey = parsedBlock.DocumentKey, DisplayOrder = parsedBlock.DisplayOrder,
                        Type = parsedBlock.Type, TextContent = parsedBlock.TextContent,
                        BinaryData = parsedBlock.BinaryData, MediaType = parsedBlock.MediaType,
                        FileName = parsedBlock.FileName
                    });
                }
            }
            groups[group.ExternalId] = group;
            exam.PassageGroups.Add(group);
        }

        var correctOptions = new Dictionary<Question, Guid>();
        foreach (var parsedQuestion in parsed.Questions.OrderBy(x => x.DisplayOrder))
        {
            var question = new Question
            {
                Id = Guid.NewGuid(),
                ExamId = exam.Id,
                ExamPartId = exam.Parts.Single(x => x.PartNumber == parsedQuestion.Part).Id,
                SourceQuestionId = parsedQuestion.SourceQuestionId ?? "q" + parsedQuestion.OriginalNumber,
                PassageGroupId = string.IsNullOrWhiteSpace(parsedQuestion.GroupExternalId) ? null : groups[parsedQuestion.GroupExternalId].Id,
                Part = parsedQuestion.Part,
                OriginalNumber = parsedQuestion.OriginalNumber,
                DisplayOrder = parsedQuestion.DisplayOrder,
                Prompt = parsedQuestion.Prompt,
                Hint = parsedQuestion.Hint,
                AllowQuestionShuffle = parsedQuestion.AllowQuestionShuffle,
                AllowOptionShuffle = parsedQuestion.AllowOptionShuffle
            };
            foreach (var parsedOption in parsedQuestion.Options.OrderBy(x => x.DisplayOrder))
            {
                var option = new QuestionOption
                {
                    Id = Guid.NewGuid(),
                    QuestionId = question.Id,
                    Key = parsedOption.Key,
                    DisplayOrder = parsedOption.DisplayOrder,
                    Text = parsedOption.Text
                };
                option.IsCorrect = string.Equals(option.Key, parsedQuestion.CorrectOptionKey, StringComparison.Ordinal);
                question.Options.Add(option);
                if (option.IsCorrect) correctOptions[question] = option.Id;
            }
            exam.Questions.Add(question);
        }

        // Resolve the circular question/correct-option FK in two saves within the same transaction.
        await _examRepository.InsertAsync(exam);
        await CurrentUnitOfWork.SaveChangesAsync();
        foreach (var pair in correctOptions) pair.Key.CorrectOptionId = pair.Value;
        await CurrentUnitOfWork.SaveChangesAsync();

        result.Succeeded = true;
        result.ExamId = exam.Id;
        return result;
    }

    [AbpAuthorize(PermissionNames.Pages_Exams_Manage)]
    [UnitOfWork(false)]
    public Task<ImportExamResultDto> ValidateWordAsync(ImportExamInput input)
    {
        EnsureHostScope();
        var result = ParseUpload(input, out var parsed);
        result.Succeeded = parsed?.IsValid == true;
        return Task.FromResult(result);
    }

    private ImportExamResultDto ParseUpload(ImportExamInput input, out ParsedExamDocument parsed)
    {
        parsed = null;
        var result = new ImportExamResultDto();
        if (input?.Content == null || input.Content.Length == 0)
        {
            result.Errors.Add(Error("Tệp", "EMPTY_FILE", "Tệp tải lên rỗng.", "Chọn tệp .docx có nội dung."));
            return result;
        }
        if (!string.Equals(Path.GetExtension(input.FileName), ".docx", StringComparison.OrdinalIgnoreCase))
        {
            result.Errors.Add(Error("Tệp", "INVALID_EXTENSION", "Chỉ chấp nhận tệp .docx.", "Lưu tài liệu bằng định dạng Word Document (.docx)."));
            return result;
        }
        if (input.Content.Length > MaxUploadBytes)
        {
            result.Errors.Add(Error("Tệp", "FILE_TOO_LARGE", "Tệp vượt quá 25 MB.", "Nén ảnh trong Word rồi tải lại."));
            return result;
        }

        parsed = _parser.Parse(input.Content);
        result.Code = parsed.Code;
        result.QuestionCount = parsed.Questions.Count;
        result.Errors = parsed.Errors.Select(x => Error(x.Location, x.Code, x.Message, x.Fix)).ToList();
        return result;
    }

    [AbpAuthorize(PermissionNames.Pages_Exams_Manage)]
    public async Task<ExamValidationResultDto> ValidateAsync(EntityDto<Guid> input)
    {
        EnsureHostScope();
        var exam = await LoadExamAsync(input.Id);
        var errors = ExamStructureValidator.Validate(exam, requireOfficial: true);
        if (exam.Status != ExamStatus.Draft)
            errors.Add(Error("EXAM", "NOT_DRAFT", "Chỉ xuất bản đề Draft.", "Sao chép đề thành Draft để tạo phiên bản mới."));
        if (exam.IsLocked || await _attemptRepository.GetAll().AnyAsync(x => x.ExamId == exam.Id))
            errors.Add(Error("EXAM", "EXAM_LOCKED", "Đề đã có lượt thi nên không thay đổi được.", "Sao chép đề thành Draft."));
        return new ExamValidationResultDto { ExamId = exam.Id, CanPublish = errors.Count == 0, Errors = errors };
    }

    [AbpAuthorize(PermissionNames.Pages_Exams_Manage)]
    public async Task<ExamDetailDto> CopyToDraftAsync(CopyExamToDraftInput input)
    {
        EnsureHostScope();
        await _transactionLock.AcquireAsync("exam:" + input.Id);
        var source = await LoadExamAsync(input.Id);
        var suffix = "-D-" + Guid.NewGuid().ToString("N").Substring(0, 8);
        var code = string.IsNullOrWhiteSpace(input.Code)
            ? source.Code.Substring(0, Math.Min(source.Code.Length, ToeicConsts.MaxCodeLength - suffix.Length)) + suffix
            : input.Code.Trim();
        var title = string.IsNullOrWhiteSpace(input.Title)
            ? source.Title.Substring(0, Math.Min(source.Title.Length, ToeicConsts.MaxTitleLength - 12)) + " (bản nháp)"
            : input.Title.Trim();
        if (code.Length > ToeicConsts.MaxCodeLength || title.Length > ToeicConsts.MaxTitleLength)
            throw new UserFriendlyException("Mã đề tối đa 64 ký tự, tên đề tối đa 256 ký tự.");
        await _transactionLock.AcquireAsync("code:" + code.ToUpperInvariant());
        if (await CodeExistsAsync(code)) throw new UserFriendlyException("Mã đề sao chép đã tồn tại. Hãy dùng mã mới.");

        var copy = ExamDraftFactory.Create(source, code, title);
        var answers = copy.Questions.ToDictionary(x => x.Id, x => x.CorrectOptionId);
        foreach (var question in copy.Questions) question.CorrectOptionId = null;
        await _examRepository.InsertAsync(copy);
        await CurrentUnitOfWork.SaveChangesAsync();
        foreach (var question in copy.Questions) question.CorrectOptionId = answers[question.Id];
        await CurrentUnitOfWork.SaveChangesAsync();
        return MapDetail(copy);
    }

    [AbpAuthorize(PermissionNames.Pages_Exams_Manage)]
    public async Task UpdateAsync(UpdateExamInput input)
    {
        EnsureHostScope();
        await _transactionLock.AcquireAsync("exam:" + input.Id);
        var exam = await _examRepository.GetAll()
            .AsSplitQuery()
            .Include(x => x.Parts)
            .Include(x => x.PassageGroups).ThenInclude(x => x.ContentBlocks)
            .Include(x => x.PassageGroups).ThenInclude(x => x.Passages)
            .Include(x => x.Questions).ThenInclude(x => x.Options)
            .FirstOrDefaultAsync(x => x.Id == input.Id);
        if (exam == null) throw new UserFriendlyException("Không tìm thấy đề thi.");
        await EnsureEditableAsync(exam);

        exam.Title = input.Title?.Trim();
        exam.DurationMinutes = input.DurationMinutes;
        if (string.IsNullOrWhiteSpace(exam.Title))
        {
            throw new UserFriendlyException("Tên đề không được để trống.");
        }

        foreach (var blockInput in input.ContentBlocks ?? new List<UpdateContentBlockInput>())
        {
            var block = exam.PassageGroups.SelectMany(x => x.ContentBlocks).SingleOrDefault(x => x.Id == blockInput.Id);
            if (block == null)
            {
                throw new UserFriendlyException("Khối nội dung không thuộc đề đang chỉnh sửa.");
            }
            if (block.Type == ContentBlockType.Image)
            {
                continue;
            }
            block.TextContent = block.Type == ContentBlockType.Table ? TableContentSanitizer.Normalize(blockInput.TextContent) : blockInput.TextContent;
            block.DisplayOrder = blockInput.DisplayOrder;
        }

        var changedAnswers = new Dictionary<Question, Guid>();
        foreach (var questionInput in input.Questions ?? new List<UpdateQuestionInput>())
        {
            var question = exam.Questions.SingleOrDefault(x => x.Id == questionInput.Id);
            if (question == null)
            {
                throw new UserFriendlyException("Câu hỏi không thuộc đề đang chỉnh sửa.");
            }

            question.Prompt = questionInput.Prompt;
            question.Hint = questionInput.Hint;
            question.AllowQuestionShuffle = questionInput.AllowQuestionShuffle;
            question.AllowOptionShuffle = questionInput.AllowOptionShuffle;

            if (questionInput.Options != null && questionInput.Options.Count > 0)
            {
                if (questionInput.Options.Count != 4 || questionInput.Options.Select(x => x.Id).Distinct().Count() != 4)
                {
                    throw new UserFriendlyException($"Câu {question.OriginalNumber} phải có đúng bốn lựa chọn khác nhau.");
                }
                foreach (var optionInput in questionInput.Options)
                {
                    var option = question.Options.SingleOrDefault(x => x.Id == optionInput.Id);
                    if (option == null || string.IsNullOrWhiteSpace(optionInput.Text))
                    {
                        throw new UserFriendlyException($"Lựa chọn của câu {question.OriginalNumber} không hợp lệ.");
                    }
                    option.Text = optionInput.Text.Trim();
                }
            }

            if (question.Options.All(x => x.Id != questionInput.CorrectOptionId))
            {
                throw new UserFriendlyException($"Đáp án đúng của câu {question.OriginalNumber} không thuộc câu hỏi.");
            }
            question.CorrectOptionId = questionInput.CorrectOptionId;
            foreach (var option in question.Options) option.IsCorrect = option.Id == question.CorrectOptionId;
            changedAnswers[question] = questionInput.CorrectOptionId;
        }

        ValidateExamStructure(exam, exam.IsOfficialFormat);
        exam.Version++;
        // Validation is complete before the first write. Clear the old filtered-unique value
        // in a separate save so changing A -> B cannot depend on SQL update order.
        if (changedAnswers.Count > 0)
        {
            foreach (var question in changedAnswers.Keys)
                foreach (var option in question.Options) option.IsCorrect = false;
            await CurrentUnitOfWork.SaveChangesAsync();
            foreach (var pair in changedAnswers)
                foreach (var option in pair.Key.Options) option.IsCorrect = option.Id == pair.Value;
        }
        await CurrentUnitOfWork.SaveChangesAsync();
    }

    [AbpAuthorize(PermissionNames.Pages_Exams_Manage)]
    public async Task ReplaceImageAsync(ReplaceExamImageInput input)
    {
        EnsureHostScope();
        var mediaType = ExamImageValidator.GetMediaType(input.Content, input.FileName);
        await _transactionLock.AcquireAsync("exam:" + input.ExamId);
        var exam = await LoadExamAsync(input.ExamId);
        await EnsureEditableAsync(exam);
        var block = exam.PassageGroups.SelectMany(x => x.ContentBlocks).SingleOrDefault(x => x.Id == input.BlockId);
        if (block == null || block.Type != ContentBlockType.Image)
            throw new UserFriendlyException("Khối ảnh không thuộc đề đang chỉnh sửa.");
        block.BinaryData = input.Content;
        block.MediaType = mediaType;
        block.FileName = Path.GetFileName(input.FileName);
        // Keep block, passage, source-page and BLANKS identities stable.
        ValidateExamStructure(exam, exam.IsOfficialFormat);
        exam.Version++;
        await CurrentUnitOfWork.SaveChangesAsync();
    }

    [AbpAuthorize(PermissionNames.Pages_Exams_Manage)]
    public async Task PublishAsync(EntityDto<Guid> input)
    {
        EnsureHostScope();
        await _transactionLock.AcquireAsync("exam:" + input.Id);
        var exam = await _examRepository.GetAll()
            .AsSplitQuery()
            .Include(x => x.Parts)
            .Include(x => x.PassageGroups).ThenInclude(x => x.ContentBlocks)
            .Include(x => x.PassageGroups).ThenInclude(x => x.Passages)
            .Include(x => x.Questions).ThenInclude(x => x.Options)
            .FirstOrDefaultAsync(x => x.Id == input.Id);
        if (exam == null) throw new UserFriendlyException("Không tìm thấy đề thi.");
        if (exam.IsLocked || await _attemptRepository.GetAll().AnyAsync(x => x.ExamId == exam.Id)) throw new UserFriendlyException("Đề đã có lượt thi nên không thể thay đổi trạng thái.");
        if (exam.Status != ExamStatus.Draft) throw new UserFriendlyException("Chỉ xuất bản đề đang ở trạng thái Draft.");

        ValidateExamStructure(exam, requireOfficial: true);

        exam.Status = ExamStatus.Published;
        exam.PublishedAtUtc = DateTime.UtcNow;
        await CurrentUnitOfWork.SaveChangesAsync();
    }

    [AbpAuthorize(PermissionNames.Pages_Exams_Statistics)]
    public async Task<ExamStatisticsDto> GetStatisticsAsync(EntityDto<Guid> input)
    {
        EnsureHostScope();
        var exam = await _examRepository.GetAsync(input.Id);
        var attempts = _attemptRepository.GetAll().Where(x => x.ExamId == input.Id);
        var completed = attempts.Where(x => x.Status != AttemptStatus.InProgress);
        var completedCount = await completed.CountAsync();
        var average = completedCount == 0
            ? 0m
            : await completed.AverageAsync(x => x.CorrectPercentage);

        var top = await (
            from answer in _attemptQuestionRepository.GetAll()
            join attempt in completed on answer.AttemptId equals attempt.Id
            join question in _questionRepository.GetAll() on answer.QuestionId equals question.Id
            group new { answer, attempt.UserId } by new { question.Id, question.OriginalNumber } into grouped
            select new QuestionErrorStatisticDto
            {
                QuestionId = grouped.Key.Id,
                OriginalNumber = grouped.Key.OriginalNumber,
                IncorrectCount = grouped.Where(x => x.answer.SelectedOptionId != null && x.answer.IsCorrect == false).Select(x => x.UserId).Distinct().Count(),
                SkippedCount = grouped.Count(x => x.answer.SelectedOptionId == null)
            })
            .Where(x => x.IncorrectCount > 0)
            .OrderByDescending(x => x.IncorrectCount)
            .ThenBy(x => x.OriginalNumber)
            .Take(10)
            .ToListAsync();

        return new ExamStatisticsDto
        {
            ExamId = exam.Id,
            ExamTitle = exam.Title,
            AttemptCount = await attempts.CountAsync(),
            CompletedAttemptCount = completedCount,
            DistinctLearnerCount = await attempts.Select(x => x.UserId).Distinct().CountAsync(),
            AverageCorrectPercentage = decimal.Round(average, 2),
            TopIncorrectQuestions = top
        };
    }

    private async Task EnsureEditableAsync(Exam exam)
    {
        if (exam.Status != ExamStatus.Draft) throw new UserFriendlyException("Chỉ sửa bản nháp. Đề đã xuất bản cần được sao chép thành bản Draft mới.");
        if (exam.IsLocked || await _attemptRepository.GetAll().AnyAsync(x => x.ExamId == exam.Id))
        {
            exam.IsLocked = true;
            throw new UserFriendlyException("Đề đã có lượt thi. Hãy tạo phiên bản đề mới thay vì sửa nội dung đang được sử dụng.");
        }
    }

    private async Task<Exam> LoadExamAsync(Guid id)
    {
        var exam = await _examRepository.GetAll().AsSplitQuery().Include(x => x.Parts)
            .Include(x => x.PassageGroups).ThenInclude(x => x.Passages)
            .Include(x => x.PassageGroups).ThenInclude(x => x.ContentBlocks)
            .Include(x => x.Questions).ThenInclude(x => x.Options)
            .FirstOrDefaultAsync(x => x.Id == id);
        return exam ?? throw new UserFriendlyException("Không tìm thấy đề thi.");
    }

    private async Task<bool> CodeExistsAsync(string code)
    {
        using (CurrentUnitOfWork.DisableFilter(AbpDataFilters.SoftDelete))
            return await _examRepository.GetAll().AnyAsync(x => x.Code == code);
    }

    private static void ValidateExamStructure(Exam exam, bool requireOfficial)
    {
        var errors = ExamStructureValidator.Validate(exam, requireOfficial);
        if (errors.Count > 0) throw new UserFriendlyException(errors[0].Location + ": " + errors[0].Message);
    }

    private static ExamDetailDto MapDetail(Exam exam)
    {
        return new ExamDetailDto
        {
            Id = exam.Id,
            SourceExamId = exam.SourceExamId,
            TagVersion = exam.TagVersion,
            Directions = exam.Directions,
            Parts = exam.Parts.OrderBy(x => x.DisplayOrder).Select(x => new ExamPartManagementDto
            {
                Number = x.PartNumber, DisplayOrder = x.DisplayOrder, Directions = x.Directions
            }).ToList(),
            Code = exam.Code,
            Title = exam.Title,
            DurationMinutes = exam.DurationMinutes,
            Status = exam.Status,
            Version = exam.Version,
            IsLocked = exam.IsLocked,
            IsOfficialFormat = exam.IsOfficialFormat,
            PublishedAtUtc = exam.PublishedAtUtc,
            QuestionCount = exam.Questions.Count,
            CompletedAttemptCount = exam.Attempts?.Count(x => x.Status != AttemptStatus.InProgress) ?? 0,
            AttemptCount = exam.Attempts?.Count ?? 0,
            Groups = exam.PassageGroups.OrderBy(x => x.DisplayOrder).Select(group => new PassageGroupManagementDto
            {
                Id = group.Id,
                ExternalId = group.ExternalId,
                Title = group.Title,
                Part = group.Part,
                DisplayOrder = group.DisplayOrder,
                AllowQuestionShuffle = group.AllowQuestionShuffle,
                FirstQuestionNumber = group.FirstQuestionNumber,
                LastQuestionNumber = group.LastQuestionNumber,
                Passages = group.Passages.OrderBy(x => x.DisplayOrder).Select(x => new PassageManagementDto
                {
                    Id = x.Id, SourcePassageId = x.SourcePassageId, Kind = x.Kind,
                    DisplayOrder = x.DisplayOrder, BlankQuestionNumbers = x.BlankQuestionNumbers
                }).ToList(),
                ContentBlocks = group.ContentBlocks.OrderBy(x => x.DisplayOrder).Select(block => new ContentBlockManagementDto
                {
                    Id = block.Id,
                    PassageId = block.PassageId,
                    SourceBlockId = block.SourceBlockId,
                    SourcePage = block.SourcePage,
                    DocumentKey = block.DocumentKey,
                    DisplayOrder = block.DisplayOrder,
                    Type = block.Type,
                    TextContent = block.TextContent,
                    MediaType = block.MediaType,
                    FileName = block.FileName,
                    ContentUrl = block.Type == ContentBlockType.Image ? "/api/toeic/content/" + block.Id : null
                }).ToList()
            }).ToList(),
            Questions = exam.Questions.OrderBy(x => x.DisplayOrder).Select(question => new QuestionManagementDto
            {
                Id = question.Id,
                SourceQuestionId = question.SourceQuestionId,
                PassageGroupId = question.PassageGroupId,
                Part = question.Part,
                OriginalNumber = question.OriginalNumber,
                DisplayOrder = question.DisplayOrder,
                Prompt = question.Prompt,
                Hint = question.Hint,
                AllowQuestionShuffle = question.AllowQuestionShuffle,
                AllowOptionShuffle = question.AllowOptionShuffle,
                CorrectOptionId = question.CorrectOptionId,
                Options = question.Options.OrderBy(x => x.DisplayOrder).Select(option => new QuestionOptionManagementDto
                {
                    Id = option.Id,
                    Key = option.Key,
                    DisplayOrder = option.DisplayOrder,
                    Text = option.Text
                }).ToList()
            }).ToList()
        };
    }

    private static ImportValidationErrorDto Error(string location, string code, string message, string fix)
    {
        return new ImportValidationErrorDto { Location = location, Code = code, Message = message, Fix = fix };
    }

    private void EnsureHostScope()
    {
        if (AbpSession.TenantId.HasValue) throw new AbpAuthorizationException("Chức năng TOEIC hiện dành cho tài khoản host của bài tập.");
    }
}
