using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Runtime.Session;
using Abp.UI;
using Abp.Domain.Uow;
using Microsoft.EntityFrameworkCore;
using NguyenTatKhoa_2380601071.Toeic.Dto;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;

namespace NguyenTatKhoa_2380601071.Toeic;

[AbpAuthorize]
[UnitOfWork(true)]
public class StudentExamAppService : NguyenTatKhoa_2380601071AppServiceBase, IStudentExamAppService
{
    private readonly IRepository<Exam, Guid> _examRepository;
    private readonly IRepository<PassageGroup, Guid> _groupRepository;
    private readonly IRepository<ExamAttempt, Guid> _attemptRepository;
    private readonly IRepository<AttemptQuestion, Guid> _attemptQuestionRepository;
    private readonly IToeicTransactionLock _transactionLock;
    private readonly AttemptFinalizer _finalizer;

    public StudentExamAppService(
        IRepository<Exam, Guid> examRepository,
        IRepository<PassageGroup, Guid> groupRepository,
        IRepository<ExamAttempt, Guid> attemptRepository,
        IRepository<AttemptQuestion, Guid> attemptQuestionRepository, IToeicTransactionLock transactionLock, AttemptFinalizer finalizer)
    {
        _examRepository = examRepository;
        _groupRepository = groupRepository;
        _attemptRepository = attemptRepository;
        _attemptQuestionRepository = attemptQuestionRepository;
        _transactionLock = transactionLock;
        _finalizer = finalizer;
    }

    public async Task<ListResultDto<PublishedExamDto>> GetPublishedExamsAsync()
    {
        EnsureHostScope();
        var items = await _examRepository.GetAll()
            .Where(x => x.Status == ExamStatus.Published)
            .OrderByDescending(x => x.PublishedAtUtc)
            .Select(x => new PublishedExamDto
            {
                Id = x.Id,
                Code = x.Code,
                Title = x.Title,
                DurationMinutes = x.DurationMinutes,
                QuestionCount = x.Questions.Count,
                CanStart = x.Questions.Count == ToeicConsts.OfficialQuestionCount && x.DurationMinutes == ToeicConsts.OfficialDurationMinutes,
                Directions = x.Directions,
                Parts = x.Parts.OrderBy(p => p.DisplayOrder).Select(p => new StudentExamPartDto
                {
                    PartNumber = p.PartNumber, Directions = p.Directions, QuestionCount = p.Questions.Count
                }).ToList()
            })
            .ToListAsync();

        return new ListResultDto<PublishedExamDto>(items);
    }

    public async Task<ListResultDto<MyExamAttemptDto>> GetMyAttemptsAsync()
    {
        EnsureHostScope();
        var userId = AbpSession.GetUserId();
        var ids = await _attemptRepository.GetAll().AsNoTracking().Where(x => x.UserId == userId)
            .OrderByDescending(x => x.StartedAtUtc).Select(x => x.Id).Take(100).ToListAsync();
        var results = new List<MyExamAttemptDto>();
        foreach (var id in ids)
        {
            await _transactionLock.AcquireAsync("attempt:" + id);
            var attempt = await GetOwnedAttemptAsync(id);
            if (attempt.Status == AttemptStatus.InProgress && DateTime.UtcNow >= attempt.DueAtUtc)
                await FinalizeAsync(attempt, AttemptStatus.Expired, attempt.DueAtUtc);
            results.Add(new MyExamAttemptDto
            {
                AttemptId = attempt.Id, ExamId = attempt.ExamId, ExamTitle = attempt.Exam.Title,
                Status = attempt.Status, StartedAtUtc = AsUtc(attempt.StartedAtUtc), DueAtUtc = AsUtc(attempt.DueAtUtc),
                SubmittedAtUtc = attempt.SubmittedAtUtc.HasValue ? AsUtc(attempt.SubmittedAtUtc.Value) : null,
                Result = attempt.Status == AttemptStatus.InProgress ? null : MapResult(attempt)
            });
        }
        await CurrentUnitOfWork.SaveChangesAsync();
        return new ListResultDto<MyExamAttemptDto>(results);
    }

    public async Task<StartExamOutput> StartAsync(StartExamInput input)
    {
        EnsureHostScope();
        var timing = Stopwatch.StartNew();
        var userId = AbpSession.GetUserId();
        // Serialize one learner's Start requests, without making an active
        // attempt wait behind another learner or a large Draft-copy operation.
        await _transactionLock.AcquireAsync($"start:{input.ExamId}:{userId}");
        var startLockMs = timing.ElapsedMilliseconds;
        var now = DateTime.UtcNow;
        // Verify publication before attempting to resume, including direct API callers.
        if (!await _examRepository.GetAll().AnyAsync(x => x.Id == input.ExamId && x.Status == ExamStatus.Published))
            throw new AbpAuthorizationException("Đề thi chưa được xuất bản hoặc không tồn tại.");
        var existing = await _attemptRepository.GetAll().AsNoTracking()
            .Where(x => x.ExamId == input.ExamId && x.UserId == userId && x.Status == AttemptStatus.InProgress)
            .OrderByDescending(x => x.StartedAtUtc)
            .FirstOrDefaultAsync();
        if (existing != null)
        {
            await _transactionLock.AcquireAsync("attempt:" + existing.Id);
            // Another request may have submitted while this request waited for the lock.
            existing = await _attemptRepository.GetAsync(existing.Id);
            now = DateTime.UtcNow;
            if (existing.Status == AttemptStatus.InProgress && now < existing.DueAtUtc)
            {
                return new StartExamOutput
                {
                    AttemptId = existing.Id,
                    StartedAtUtc = AsUtc(existing.StartedAtUtc),
                    DueAtUtc = AsUtc(existing.DueAtUtc),
                    ResumedExistingAttempt = true
                };
            }
            await FinalizeAsync(existing, AttemptStatus.Expired, existing.DueAtUtc);
            await CurrentUnitOfWork.SaveChangesAsync();
        }

        var examLockStarted = timing.ElapsedMilliseconds;
        await _transactionLock.AcquireAsync("exam:" + input.ExamId);
        var examLockMs = timing.ElapsedMilliseconds - examLockStarted;
        // Publication is rechecked under the same lock used by management.
        var exam = await _examRepository.GetAll()
            .AsSplitQuery()
            .Include(x => x.PassageGroups)
            .Include(x => x.Questions).ThenInclude(x => x.Options)
            .FirstOrDefaultAsync(x => x.Id == input.ExamId);
        if (exam == null) throw new UserFriendlyException("Không tìm thấy đề thi.");
        if (exam.Status != ExamStatus.Published)
        {
            throw new AbpAuthorizationException("Đề thi chưa được xuất bản.");
        }
        if (exam.Questions.Count != ToeicConsts.OfficialQuestionCount || exam.DurationMinutes != ToeicConsts.OfficialDurationMinutes)
        {
            throw new UserFriendlyException("Đề thi chưa đủ 100 câu nên chưa thể bắt đầu.");
        }
        var loadCompletedMs = timing.ElapsedMilliseconds;

        var attempt = new ExamAttempt
        {
            Id = Guid.NewGuid(),
            ExamId = exam.Id,
            UserId = userId,
            DurationMinutesSnapshot = exam.DurationMinutes,
            StartedAtUtc = now,
            DueAtUtc = now.AddMinutes(exam.DurationMinutes),
            Status = AttemptStatus.InProgress,
            TotalQuestions = exam.Questions.Count
        };

        var random = new Random(BitConverter.ToInt32(attempt.Id.ToByteArray(), 0));
        var orderedQuestions = BuildQuestionOrder(exam, random);
        for (var index = 0; index < orderedQuestions.Count; index++)
        {
            var question = orderedQuestions[index];
            var optionIds = question.Options.OrderBy(x => x.DisplayOrder).Select(x => x.Id).ToList();
            if (question.AllowOptionShuffle)
            {
                Shuffle(optionIds, random);
            }
            var attemptQuestion = new AttemptQuestion
            {
                Id = Guid.NewGuid(),
                AttemptId = attempt.Id,
                ExamId = exam.Id,
                QuestionId = question.Id,
                DisplayOrder = index + 1,
                OptionOrderCsv = string.Join(",", optionIds.Select(x => x.ToString("N")))
            };
            for (var position = 0; position < optionIds.Count; position++)
            {
                attemptQuestion.Choices.Add(new AttemptChoice { Id = Guid.NewGuid(),
                    AttemptQuestionId = attemptQuestion.Id, QuestionId = question.Id,
                    ChoiceId = optionIds[position], DisplayOrder = position + 1 });
            }
            attempt.Questions.Add(attemptQuestion);
        }

        exam.IsLocked = true;
        await _attemptRepository.InsertAsync(attempt);
        await CurrentUnitOfWork.SaveChangesAsync();

        // Creating 100 question snapshots and 400 choice rows can be slow on a
        // cold server. Start the student's full duration only once those rows
        // are ready, atomically in this SAME transaction. Resume never resets it.
        attempt.StartedAtUtc = DateTime.UtcNow;
        attempt.DueAtUtc = attempt.StartedAtUtc.AddMinutes(attempt.DurationMinutesSnapshot);
        await CurrentUnitOfWork.SaveChangesAsync();

        if (timing.ElapsedMilliseconds > 2000)
            Logger.Warn($"Slow TOEIC Start: learnerLock={startLockMs}ms, examLock={examLockMs}ms, snapshotSave={timing.ElapsedMilliseconds - loadCompletedMs}ms, total={timing.ElapsedMilliseconds}ms.");

        return new StartExamOutput
        {
            AttemptId = attempt.Id,
            StartedAtUtc = attempt.StartedAtUtc,
            DueAtUtc = attempt.DueAtUtc,
            ResumedExistingAttempt = false
        };
    }

    public async Task<AttemptDto> GetAttemptAsync(GetAttemptInput input)
    {
        EnsureHostScope();
        await _transactionLock.AcquireAsync("attempt:" + input.AttemptId);
        var attempt = await GetOwnedAttemptAsync(input.AttemptId);
        var now = DateTime.UtcNow;
        if (attempt.Status == AttemptStatus.InProgress && now >= attempt.DueAtUtc)
        {
            await FinalizeAsync(attempt, AttemptStatus.Expired, attempt.DueAtUtc);
            await CurrentUnitOfWork.SaveChangesAsync();
        }

        var answers = await _attemptQuestionRepository.GetAll()
            .AsSplitQuery()
            .Where(x => x.AttemptId == attempt.Id)
            .Include(x => x.Question).ThenInclude(x => x.Options)
            .Include(x => x.Choices)
            .OrderBy(x => x.DisplayOrder)
            .ToListAsync();
        // Project metadata/text only: image bytes are served by the authenticated content endpoint.
        var groups = await _groupRepository.GetAll().Where(x => x.ExamId == attempt.ExamId)
            .OrderBy(x => x.DisplayOrder).Select(group => new StudentPassageGroupDto
            {
                Id = group.Id, Title = group.Title, Part = group.Part, DisplayOrder = group.DisplayOrder,
                ContentBlocks = group.ContentBlocks.OrderBy(block => block.DisplayOrder).Select(block => new StudentContentBlockDto
                {
                    Id = block.Id, DocumentKey = block.DocumentKey, DisplayOrder = block.DisplayOrder,
                    Type = block.Type, TextContent = block.TextContent, MediaType = block.MediaType,
                    ContentUrl = block.Type == ContentBlockType.Image ? "/api/toeic/content/" + block.Id : null
                }).ToList()
            }).ToListAsync();
        var parts = await _examRepository.GetAll().Where(x => x.Id == attempt.ExamId)
            .SelectMany(x => x.Parts).OrderBy(x => x.DisplayOrder)
            .Select(x => new StudentExamPartDto { PartNumber = x.PartNumber, Directions = x.Directions, QuestionCount = x.Questions.Count }).ToListAsync();

        return new AttemptDto
        {
            AttemptId = attempt.Id,
            ExamId = attempt.ExamId,
            ExamTitle = attempt.Exam.Title,
            Directions = attempt.Exam.Directions,
            Parts = parts,
            ServerNowUtc = now,
            StartedAtUtc = AsUtc(attempt.StartedAtUtc),
            DueAtUtc = AsUtc(attempt.DueAtUtc),
            SubmittedAtUtc = attempt.SubmittedAtUtc.HasValue ? AsUtc(attempt.SubmittedAtUtc.Value) : null,
            Status = attempt.Status,
            Groups = groups,
            Questions = answers.Select(MapQuestion).ToList(),
            Result = attempt.Status == AttemptStatus.InProgress ? null : MapResult(attempt)
        };
    }

    public async Task<SaveAnswerOutput> SaveAnswerAsync(SaveAnswerInput input)
    {
        EnsureHostScope();
        await _transactionLock.AcquireAsync("attempt:" + input.AttemptId);
        var attempt = await GetOwnedAttemptAsync(input.AttemptId);
        var now = DateTime.UtcNow;
        if (attempt.Status != AttemptStatus.InProgress)
        {
            throw new UserFriendlyException("Lượt thi đã được nộp, không thể thay đổi đáp án.");
        }
        if (now >= attempt.DueAtUtc)
        {
            await FinalizeAsync(attempt, AttemptStatus.Expired, attempt.DueAtUtc);
            await CurrentUnitOfWork.SaveChangesAsync();
            // Return without throwing: an exception here would roll back the finalization.
            return new SaveAnswerOutput { Accepted = false, IsFinalized = true, Result = MapResult(attempt) };
        }

        var answer = await _attemptQuestionRepository.GetAll()
            .Include(x => x.Question).ThenInclude(x => x.Options)
            .FirstOrDefaultAsync(x => x.AttemptId == attempt.Id && x.QuestionId == input.QuestionId);
        if (answer == null)
        {
            throw new UserFriendlyException("Câu hỏi không thuộc lượt thi này.");
        }
        if (input.SelectedOptionId.HasValue && answer.Question.Options.All(x => x.Id != input.SelectedOptionId.Value))
        {
            throw new UserFriendlyException("Lựa chọn không thuộc câu hỏi.");
        }

        answer.SelectedOptionId = input.SelectedOptionId;
        answer.AnsweredAtUtc = input.SelectedOptionId.HasValue ? now : null;
        answer.IsCorrect = null;
        await CurrentUnitOfWork.SaveChangesAsync();
        return new SaveAnswerOutput { Accepted = true, IsFinalized = false };
    }

    public async Task<AttemptResultDto> SubmitAsync(SubmitAttemptInput input)
    {
        EnsureHostScope();
        await _transactionLock.AcquireAsync("attempt:" + input.AttemptId);
        var attempt = await GetOwnedAttemptAsync(input.AttemptId);
        if (attempt.Status == AttemptStatus.InProgress)
        {
            var now = DateTime.UtcNow;
            var finalStatus = now >= attempt.DueAtUtc ? AttemptStatus.Expired : AttemptStatus.Submitted;
            await FinalizeAsync(attempt, finalStatus, finalStatus == AttemptStatus.Expired ? attempt.DueAtUtc : now);
            await CurrentUnitOfWork.SaveChangesAsync();
        }

        return MapResult(attempt);
    }

    private async Task<ExamAttempt> GetOwnedAttemptAsync(Guid id)
    {
        var userId = AbpSession.GetUserId();
        var attempt = await _attemptRepository.GetAll().Include(x => x.Exam)
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);
        if (attempt == null)
        {
            throw new AbpAuthorizationException("Không tìm thấy lượt thi hoặc bạn không có quyền truy cập.");
        }
        return attempt;
    }

    private async Task FinalizeAsync(ExamAttempt attempt, AttemptStatus status, DateTime submittedAtUtc)
    {
        await _finalizer.FinalizeAsync(attempt, status, submittedAtUtc);
    }

    private static List<Question> BuildQuestionOrder(Exam exam, Random random)
    {
        var result = new List<Question>();
        var part5 = exam.Questions.Where(x => x.Part == ToeicPart.Part5).OrderBy(x => x.DisplayOrder).ToList();
        ShuffleEligible(part5, x => x.AllowQuestionShuffle, random);
        result.AddRange(part5);

        foreach (var part in new[] { ToeicPart.Part6, ToeicPart.Part7 })
        {
            foreach (var group in exam.PassageGroups.Where(x => x.Part == part).OrderBy(x => x.DisplayOrder))
            {
                var groupQuestions = exam.Questions.Where(x => x.PassageGroupId == group.Id).OrderBy(x => x.DisplayOrder).ToList();
                // Part 6/7 child order and group order are fixed, even when source flags are stored.
                result.AddRange(groupQuestions);
            }
        }
        return result;
    }

    private static void ShuffleEligible<T>(IList<T> items, Func<T, bool> canShuffle, Random random)
    {
        var indexes = Enumerable.Range(0, items.Count).Where(i => canShuffle(items[i])).ToList();
        var values = indexes.Select(i => items[i]).ToList();
        Shuffle(values, random);
        for (var i = 0; i < indexes.Count; i++) items[indexes[i]] = values[i];
    }

    private static void Shuffle<T>(IList<T> items, Random random)
    {
        for (var i = items.Count - 1; i > 0; i--)
        {
            var j = random.Next(i + 1);
            (items[i], items[j]) = (items[j], items[i]);
        }
    }

    private static StudentPassageGroupDto MapGroup(PassageGroup group)
    {
        return new StudentPassageGroupDto
        {
            Id = group.Id,
            Title = group.Title,
            Part = group.Part,
            DisplayOrder = group.DisplayOrder,
            ContentBlocks = group.ContentBlocks.OrderBy(x => x.DisplayOrder).Select(block => new StudentContentBlockDto
            {
                Id = block.Id,
                DocumentKey = block.DocumentKey,
                DisplayOrder = block.DisplayOrder,
                Type = block.Type,
                TextContent = block.TextContent,
                MediaType = block.MediaType,
                ContentUrl = block.Type == ContentBlockType.Image ? "/api/toeic/content/" + block.Id : null
            }).ToList()
        };
    }

    private static StudentQuestionDto MapQuestion(AttemptQuestion answer)
    {
        var optionById = answer.Question.Options.ToDictionary(x => x.Id);
        var ids = answer.Choices.Count > 0 ? answer.Choices.OrderBy(x => x.DisplayOrder).Select(x => x.ChoiceId)
            : answer.OptionOrderCsv.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(x => Guid.ParseExact(x, "N"));
        var orderedOptions = ids
            .Where(optionById.ContainsKey)
            .Select(x => optionById[x])
            .ToList();
        return new StudentQuestionDto
        {
            Id = answer.QuestionId,
            PassageGroupId = answer.Question.PassageGroupId,
            Part = answer.Question.Part,
            OriginalNumber = answer.Question.OriginalNumber,
            DisplayOrder = answer.DisplayOrder,
            Prompt = answer.Question.Prompt,
            SelectedOptionId = answer.SelectedOptionId,
            Options = orderedOptions.Select((x, index) => new StudentQuestionOptionDto
            {
                Id = x.Id,
                Key = ((char)('A' + index)).ToString(),
                Text = x.Text
            }).ToList()
        };
    }

    private static AttemptResultDto MapResult(ExamAttempt attempt)
    {
        return new AttemptResultDto
        {
            AttemptId = attempt.Id,
            Status = attempt.Status,
            SubmittedAtUtc = DateTime.SpecifyKind(attempt.SubmittedAtUtc ?? attempt.DueAtUtc, DateTimeKind.Utc),
            TotalQuestions = attempt.TotalQuestions,
            CorrectCount = attempt.CorrectCount,
            IncorrectCount = attempt.IncorrectCount,
            SkippedCount = attempt.SkippedCount,
            CorrectPercentage = attempt.CorrectPercentage
        };
    }

    private void EnsureHostScope()
    {
        if (AbpSession.TenantId.HasValue) throw new AbpAuthorizationException("Chức năng TOEIC hiện dành cho tài khoản host của bài tập.");
    }

    private static DateTime AsUtc(DateTime value) => DateTime.SpecifyKind(value, DateTimeKind.Utc);
}
