using Abp.Application.Services.Dto;
using Abp.Dependency;
using Abp.Domain.Uow;
using Abp.EntityFrameworkCore;
using Abp.Runtime.Session;
using Abp.UI;
using Microsoft.EntityFrameworkCore;
using NguyenTatKhoa_2380601071.EntityFrameworkCore;
using NguyenTatKhoa_2380601071.Toeic;
using NguyenTatKhoa_2380601071.Toeic.Dto;

namespace Toeic.DatabaseCheck;

public static class FullIntegrationChecks
{
    public static async Task RunAsync(IIocResolver resolver)
    {
        var sourcePath = Path.Combine(AppContext.BaseDirectory, "SourceFixtures", "TOEIC_Reading_v07_co_tag.docx");
        if (!File.Exists(sourcePath)) throw new FileNotFoundException("The supplied Reading v07 Word source is required.", sourcePath);
        var manager = resolver.Resolve<IUnitOfWorkManager>();
        using var scope = manager.Begin(new UnitOfWorkOptions { IsTransactional = true });
        var context = await resolver.Resolve<IDbContextProvider<NguyenTatKhoa_2380601071DbContext>>().GetDbContextAsync();
        var userId = await context.Users.Where(x => x.TenantId == null && x.UserName == "admin").Select(x => x.Id).SingleAsync();
        using var identity = resolver.Resolve<IAbpSession>().Use(null, userId);
        var management = resolver.Resolve<IExamManagementAppService>();
        var input = new ImportExamInput { FileName = Path.GetFileName(sourcePath), Content = await File.ReadAllBytesAsync(sourcePath) };
        var valid = await management.ValidateWordAsync(input);
        Require(valid.Succeeded && valid.QuestionCount == 100 && valid.ExamId == null, "validate-only Word v07 returns 100 questions without writing an exam");
        var imported = await management.ImportAsync(input);
        Require(imported.Succeeded && imported.ExamId.HasValue, "real Word v07 import succeeds");
        var id = imported.ExamId.Value;
        var repeated = await management.ImportAsync(input);
        Require(repeated.Succeeded && repeated.AlreadyImported && repeated.ExamId == id, "re-importing the same document does not duplicate it");
        var detail = await management.GetAsync(new EntityDto<Guid>(id));
        Require(detail.QuestionCount == 100 && detail.Groups.Count == 19 && detail.Groups.Sum(g => g.ContentBlocks.Count(b => b.Type == ContentBlockType.Image)) == 27, "real import has 100 questions, 19 groups, 27 image blocks");
        Require(await context.Passages.CountAsync(p => p.PassageGroup.ExamId == id) == 27 && await context.QuestionOptions.CountAsync(o => o.Question.ExamId == id) == 400, "27 passages and 400 options persisted");
        Require(await context.Questions.CountAsync(q => q.ExamId == id && q.Hint != null && q.Hint != "") == 100, "all 100 source hints preserved");
        Require(await context.Questions.CountAsync(q => q.ExamId == id && q.Part == ToeicPart.Part6 && (q.Prompt == null || q.Prompt == "")) == 16, "Part 6 accepts the 16 empty stems from source");
        Require(await context.Questions.CountAsync(q => q.ExamId == id && (q.OriginalNumber == 163 || q.OriginalNumber == 167) && !q.AllowOptionShuffle) == 2, "source option flags for questions 163/167 preserved");
        var beforeInvalid = await context.Exams.CountAsync();
        var invalid = await management.ImportAsync(new ImportExamInput { FileName = "invalid.docx", Content = new byte[] { 0, 1, 2 } });
        Require(!invalid.Succeeded && await context.Exams.CountAsync() == beforeInvalid, "invalid Word rejects all writes");
        var copied = await management.CopyToDraftAsync(new CopyExamToDraftInput { Id = id, Code = "E2E-ROLLBACK-" + Guid.NewGuid().ToString("N"), Title = "Kiểm thử rollback — Reading v07" });
        Require(copied.Status == ExamStatus.Draft && copied.Id != id && copied.Questions.Count == 100 && !copied.Questions.Select(q => q.Id).Intersect(detail.Questions.Select(q => q.Id)).Any(), "Draft copy allocates new IDs and preserves the source");
        var student = resolver.Resolve<IStudentExamAppService>();
        try { await student.StartAsync(new StartExamInput { ExamId = copied.Id }); throw new InvalidOperationException("Draft exam was allowed to start."); }
        catch (Abp.Authorization.AbpAuthorizationException) { Console.WriteLine("PASS: unpublished exam cannot start"); }
        await management.UpdateAsync(new UpdateExamInput { Id = copied.Id, Title = "Kiểm thử đã chỉnh sửa — Reading v07", DurationMinutes = 75 });
        var image = copied.Groups.First().ContentBlocks.First(x => x.Type == ContentBlockType.Image);
        var originalBytes = await context.ContentBlocks.Where(x => x.Id == image.Id).Select(x => x.BinaryData).SingleAsync();
        await management.ReplaceImageAsync(new ReplaceExamImageInput { ExamId = copied.Id, BlockId = image.Id, FileName = "verified-replacement.png", Content = originalBytes });
        Require((await management.GetAsync(new EntityDto<Guid>(copied.Id))).Groups.SelectMany(x => x.ContentBlocks).Single(x => x.Id == image.Id).SourceBlockId == image.SourceBlockId, "image replacement preserves block and passage identities");
        var validation = await management.ValidateAsync(new EntityDto<Guid>(copied.Id));
        Require(validation.CanPublish, "edited real-source Draft validates before publication");
        await management.PublishAsync(new EntityDto<Guid>(copied.Id));
        try { await management.ReplaceImageAsync(new ReplaceExamImageInput { ExamId = copied.Id, BlockId = image.Id, FileName = "locked.png", Content = originalBytes }); throw new InvalidOperationException("Published image was editable."); }
        catch (UserFriendlyException) { Console.WriteLine("PASS: published images cannot be replaced"); }
        var started = await student.StartAsync(new StartExamInput { ExamId = copied.Id });
        var attempt = await student.GetAttemptAsync(new GetAttemptInput { AttemptId = started.AttemptId });
        Require(attempt.Questions.Count == 100 && attempt.Groups.Count == 19 && attempt.Parts.Count == 3, "student API returns three parts and grouped real-source content");
        Require((await student.GetMyAttemptsAsync()).Items.Any(x => x.AttemptId == started.AttemptId), "attempt history belongs to the current learner");
        var q = attempt.Questions.First();
        await student.SaveAnswerAsync(new SaveAnswerInput { AttemptId = started.AttemptId, QuestionId = q.Id, SelectedOptionId = q.Options[0].Id });
        var result = await student.SubmitAsync(new SubmitAttemptInput { AttemptId = started.AttemptId });
        Require(result.CorrectCount + result.IncorrectCount == 1 && result.SkippedCount == 99, "real-source exam is graded and stored server-side");
        var statistics = await management.GetStatisticsAsync(new EntityDto<Guid>(copied.Id));
        Require(statistics.CompletedAttemptCount == 1, "lecturer statistics use the completed attempt");
        try { await management.UpdateAsync(new UpdateExamInput { Id = copied.Id, Title = "Must not save", DurationMinutes = 75 }); throw new InvalidOperationException("Used exam was editable."); }
        catch (UserFriendlyException) { Console.WriteLine("PASS: used/published exam cannot be edited"); }

        var removedExam = await context.Exams.SingleAsync(x => x.Id == copied.Id);
        removedExam.IsDeleted = true;
        removedExam.DeletionTime = DateTime.UtcNow;
        await context.SaveChangesAsync();
        Require(!(await student.GetMyAttemptsAsync()).Items.Any(x => x.AttemptId == started.AttemptId),
            "soft-deleted exams are omitted from learner history without failing the request");
        Require(await context.ExamAttempts.AnyAsync(x => x.Id == started.AttemptId && x.SubmittedAtUtc == result.SubmittedAtUtc),
            "removing an exam preserves its stored attempt and submitted result");
        removedExam.IsDeleted = false;
        removedExam.DeletionTime = null;
        await context.SaveChangesAsync();
        Require((await student.GetMyAttemptsAsync()).Items.Any(x => x.AttemptId == started.AttemptId),
            "restoring an exam restores its learner history entry");
        Console.WriteLine("PASS: full Word v07 import → preview → edit → publish → answer → submit → statistics; all scenario data will roll back.");
    }

    private static void Require(bool value, string message)
    { if (!value) throw new InvalidOperationException("FAILED: " + message); Console.WriteLine("PASS: " + message); }
}
