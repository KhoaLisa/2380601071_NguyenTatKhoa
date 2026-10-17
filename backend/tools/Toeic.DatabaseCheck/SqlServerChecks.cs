using Abp;
using Abp.Dependency;
using Abp.Domain.Uow;
using Abp.EntityFrameworkCore;
using Abp.Runtime.Session;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NguyenTatKhoa_2380601071.EntityFrameworkCore;
using NguyenTatKhoa_2380601071.Toeic;
using NguyenTatKhoa_2380601071.Toeic.Dto;
using System.Transactions;

namespace Toeic.DatabaseCheck;

public static class SqlServerChecks
{
    public static async Task RunAsync(IIocResolver resolver)
    {
        var manager = resolver.Resolve<IUnitOfWorkManager>();
        using var session = resolver.Resolve<IAbpSession>().Use(null, null);
        // The entire scenario is rolled back: no synthetic exams, answers or statistics remain.
        using var scenario = manager.Begin(new UnitOfWorkOptions { IsTransactional = true, IsolationLevel = IsolationLevel.ReadCommitted });
        var context = await resolver.Resolve<IDbContextProvider<NguyenTatKhoa_2380601071DbContext>>().GetDbContextAsync();
        var hostUserId = await context.Users.Where(x => x.TenantId == null && x.UserName == "admin").Select(x => x.Id).SingleAsync();
        using var authenticated = resolver.Resolve<IAbpSession>().Use(null, hostUserId);
        var exam = CreateExam();
        context.Exams.Add(exam);
        await context.SaveChangesAsync();
        foreach (var question in exam.Questions) question.CorrectOptionId = question.Options.Single(x => x.IsCorrect).Id;
        await context.SaveChangesAsync();

        var service = resolver.Resolve<IStudentExamAppService>();
        var started = await service.StartAsync(new StartExamInput { ExamId = exam.Id });
        var first = await service.GetAttemptAsync(new GetAttemptInput { AttemptId = started.AttemptId });
        Require(first.Questions.Count == 100, "100 questions created per attempt");
        Require(await context.AttemptChoices.CountAsync(x => x.AttemptQuestion.AttemptId == started.AttemptId) == 400, "400 persisted attempt choices");
        var resumed = await service.StartAsync(new StartExamInput { ExamId = exam.Id });
        Require(resumed.ResumedExistingAttempt && resumed.AttemptId == started.AttemptId, "start resumes the active attempt");
        var correctQuestion = first.Questions[0];
        var correctId = exam.Questions.Single(x => x.Id == correctQuestion.Id).CorrectOptionId;
        await service.SaveAnswerAsync(new SaveAnswerInput { AttemptId = started.AttemptId, QuestionId = correctQuestion.Id, SelectedOptionId = correctId });
        var wrongQuestion = first.Questions[1];
        var wrongId = exam.Questions.Single(x => x.Id == wrongQuestion.Id).Options.First(x => !x.IsCorrect).Id;
        await service.SaveAnswerAsync(new SaveAnswerInput { AttemptId = started.AttemptId, QuestionId = wrongQuestion.Id, SelectedOptionId = wrongId });
        var cancelledQuestion = first.Questions[2];
        await service.SaveAnswerAsync(new SaveAnswerInput { AttemptId = started.AttemptId, QuestionId = cancelledQuestion.Id, SelectedOptionId = cancelledQuestion.Options[0].Id });
        await service.SaveAnswerAsync(new SaveAnswerInput { AttemptId = started.AttemptId, QuestionId = cancelledQuestion.Id, SelectedOptionId = null });
        var restored = await service.GetAttemptAsync(new GetAttemptInput { AttemptId = started.AttemptId });
        Require(restored.Questions[0].SelectedOptionId == correctId && restored.Questions[2].SelectedOptionId == null, "save/change/cancel restored from database");
        Require(first.Questions.Select(q => q.Id).SequenceEqual(restored.Questions.Select(q => q.Id)) &&
                first.Questions.SelectMany(q => q.Options.Select(o => o.Id)).SequenceEqual(restored.Questions.SelectMany(q => q.Options.Select(o => o.Id))), "question and option order survives reload");
        Require(first.Questions.Where(q => q.Part != ToeicPart.Part5).Select(q => q.OriginalNumber).SequenceEqual(Enumerable.Range(131, 70)), "Part 6/7 child order remains fixed");
        Require(!System.Text.Json.JsonSerializer.Serialize(first).Contains("CorrectOptionId") && !System.Text.Json.JsonSerializer.Serialize(first).Contains("Hint"), "attempt DTO has no answer or hint fields");

        await ExpectServiceRejection(async () => await service.SaveAnswerAsync(new SaveAnswerInput { AttemptId = started.AttemptId, QuestionId = correctQuestion.Id, SelectedOptionId = wrongId }), "service rejects another question's choice");
        var answer = await context.AttemptQuestions.SingleAsync(x => x.AttemptId == started.AttemptId && x.QuestionId == correctQuestion.Id);
        await ExpectSqlRejection(context, "foreign_choice", async () => {
            await context.Database.ExecuteSqlInterpolatedAsync($"UPDATE ToeicAttemptQuestions SET SelectedOptionId={wrongId} WHERE Id={answer.Id}");
        }, "composite FK rejects another question's choice");
        var incorrectOption = exam.Questions.First().Options.First(x => !x.IsCorrect);
        var blockId = exam.PassageGroups.First().ContentBlocks.First().Id;
        await ExpectSqlRejection(context, "null_image", async () => {
            await context.Database.ExecuteSqlInterpolatedAsync($"UPDATE ToeicContentBlocks SET Type=2, TextContent=NULL, BinaryData=NULL, MediaType=NULL WHERE Id={blockId}");
        }, "image CHECK rejects NULL bytes and MIME");
        await ExpectSqlRejection(context, "two_correct", async () => {
            await context.Database.ExecuteSqlInterpolatedAsync($"UPDATE ToeicQuestionOptions SET IsCorrect=1 WHERE Id={incorrectOption.Id}");
        }, "filtered unique index rejects two correct choices");

        var submitted = await service.SubmitAsync(new SubmitAttemptInput { AttemptId = started.AttemptId });
        Require(submitted.CorrectCount == 1 && submitted.IncorrectCount == 1 && submitted.SkippedCount == 98 && submitted.CorrectPercentage == 1m, "mixed result: 1 correct + 1 incorrect + 98 skipped = 100; 1%");
        var submittedAgain = await service.SubmitAsync(new SubmitAttemptInput { AttemptId = started.AttemptId });
        Require(submitted.SubmittedAtUtc == submittedAgain.SubmittedAtUtc && submitted.CorrectCount == submittedAgain.CorrectCount, "duplicate submit returns the same result");
        await ExpectServiceRejection(async () => await service.SaveAnswerAsync(new SaveAnswerInput { AttemptId = started.AttemptId, QuestionId = correctQuestion.Id, SelectedOptionId = null }), "submitted attempt rejects later writes");

        var blankStarted = await service.StartAsync(new StartExamInput { ExamId = exam.Id });
        var blank = await service.SubmitAsync(new SubmitAttemptInput { AttemptId = blankStarted.AttemptId });
        Require(blank.CorrectCount == 0 && blank.IncorrectCount == 0 && blank.SkippedCount == 100 && blank.CorrectPercentage == 0, "all-blank result: 100 skipped; 0%");
        var expiredStart = await service.StartAsync(new StartExamInput { ExamId = exam.Id });
        var expired = await context.ExamAttempts.SingleAsync(x => x.Id == expiredStart.AttemptId);
        expired.StartedAtUtc = DateTime.UtcNow.AddMinutes(-76);
        expired.DueAtUtc = DateTime.UtcNow.AddMinutes(-1);
        await context.SaveChangesAsync();
        var expiredOutput = await service.GetAttemptAsync(new GetAttemptInput { AttemptId = expiredStart.AttemptId });
        Require(expiredOutput.Status == AttemptStatus.Expired && expiredOutput.Result.SkippedCount == 100, "server finalizes an expired attempt on request");

        var stats = await resolver.Resolve<IExamManagementAppService>().GetStatisticsAsync(new Abp.Application.Services.Dto.EntityDto<Guid>(exam.Id));
        Require(stats.CompletedAttemptCount == 3 && stats.DistinctLearnerCount == 1 && stats.AverageCorrectPercentage == 0.33m, "statistics use completed attempts and distinct learners");

        // No Complete: disposing the outer UoW rolls back every row created by these checks.
        Console.WriteLine("PASS: SQL Server and service scenarios completed; rolling back all test data.");
    }

    private static async Task ExpectSqlRejection(NguyenTatKhoa_2380601071DbContext context, string savepoint, Func<Task> action, string label)
    {
        var transaction = context.Database.CurrentTransaction ?? throw new InvalidOperationException("SQL checks require a transaction.");
        await transaction.CreateSavepointAsync(savepoint);
        try { await action(); }
        catch (SqlException e) when (e.Number is 547 or 2601 or 2627)
        {
            await transaction.RollbackToSavepointAsync(savepoint);
            Console.WriteLine("PASS: " + label);
            return;
        }
        throw new InvalidOperationException("Expected database constraint rejection: " + label);
    }

    private static async Task ExpectServiceRejection(Func<Task> action, string label)
    {
        try { await action(); }
        catch (Abp.UI.UserFriendlyException) { Console.WriteLine("PASS: " + label); return; }
        throw new InvalidOperationException("Expected service rejection: " + label);
    }

    private static void Require(bool condition, string label)
    {
        if (!condition) throw new InvalidOperationException("FAILED: " + label);
        Console.WriteLine("PASS: " + label);
    }

    internal static Exam CreateExam()
    {
        var exam = new Exam { Id = Guid.NewGuid(), Code = "ROLLBACK-" + Guid.NewGuid().ToString("N"), Title = "ROLLBACK DATABASE TEST", SourceExamId = "rollback-test", DeclaredQuestionCount = 100, Status = ExamStatus.Published, PublishedAtUtc = DateTime.UtcNow, IsOfficialFormat = true };
        foreach (var part in new[] { ToeicPart.Part5, ToeicPart.Part6, ToeicPart.Part7 })
            exam.Parts.Add(new ExamPart { Id = Guid.NewGuid(), ExamId = exam.Id, PartNumber = part, DisplayOrder = (int)part - 4 });
        var groups = new List<PassageGroup>();
        foreach (var part in new[] { ToeicPart.Part6, ToeicPart.Part7 })
        {
            var group = new PassageGroup { Id = Guid.NewGuid(), ExamId = exam.Id, ExamPartId = exam.Parts.Single(x => x.PartNumber == part).Id, Part = part, ExternalId = "g" + (int)part, DisplayOrder = (int)part - 5, FirstQuestionNumber = part == ToeicPart.Part6 ? 131 : 147, LastQuestionNumber = part == ToeicPart.Part6 ? 146 : 200 };
            var passage = new Passage { Id = Guid.NewGuid(), PassageGroupId = group.Id, SourcePassageId = "p" + (int)part, Kind = "test", DisplayOrder = 1 };
            group.Passages.Add(passage);
            group.ContentBlocks.Add(new ContentBlock { Id = Guid.NewGuid(), PassageId = passage.Id, PassageGroupId = group.Id, SourceBlockId = "test-text", DocumentKey = passage.SourcePassageId, Type = ContentBlockType.Text, TextContent = "Synthetic rollback data", DisplayOrder = 1 });
            exam.PassageGroups.Add(group);
            groups.Add(group);
        }
        for (var number = 101; number <= 200; number++)
        {
            var part = number <= 130 ? ToeicPart.Part5 : number <= 146 ? ToeicPart.Part6 : ToeicPart.Part7;
            var question = new Question { Id = Guid.NewGuid(), ExamId = exam.Id, ExamPartId = exam.Parts.Single(x => x.PartNumber == part).Id, SourceQuestionId = "q" + number, OriginalNumber = number, Part = part, DisplayOrder = number - 100, PassageGroupId = groups.FirstOrDefault(x => x.Part == part)?.Id, Prompt = "Rollback check " + number, Hint = "Technical test", AllowQuestionShuffle = part == ToeicPart.Part5, AllowOptionShuffle = number is not 163 and not 167 };
            for (var index = 0; index < 4; index++) question.Options.Add(new QuestionOption { Id = Guid.NewGuid(), QuestionId = question.Id, Key = ((char)('A' + index)).ToString(), DisplayOrder = index + 1, Text = "Test " + index, IsCorrect = index == 0 });
            exam.Questions.Add(question);
        }
        return exam;
    }
}
