using Abp.Dependency;
using Abp.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace NguyenTatKhoa_2380601071.Toeic;

// Call only while holding the transaction-owned lock for this attempt.
public class AttemptFinalizer : ITransientDependency
{
    private readonly IRepository<AttemptQuestion, Guid> _answers;
    public AttemptFinalizer(IRepository<AttemptQuestion, Guid> answers) => _answers = answers;

    public async Task FinalizeAsync(ExamAttempt attempt, AttemptStatus status, DateTime submittedAtUtc)
    {
        if (attempt.Status != AttemptStatus.InProgress) return;
        var answers = await _answers.GetAll().Where(x => x.AttemptId == attempt.Id).Include(x => x.Question).ToListAsync();
        var correct = 0;
        var incorrect = 0;
        var skipped = 0;
        foreach (var answer in answers)
        {
            if (!answer.SelectedOptionId.HasValue) { answer.IsCorrect = null; skipped++; }
            else if (answer.SelectedOptionId == answer.Question.CorrectOptionId) { answer.IsCorrect = true; correct++; }
            else { answer.IsCorrect = false; incorrect++; }
        }
        attempt.Status = status;
        attempt.SubmittedAtUtc = DateTime.SpecifyKind(submittedAtUtc, DateTimeKind.Utc);
        attempt.TotalQuestions = answers.Count;
        attempt.CorrectCount = correct;
        attempt.IncorrectCount = incorrect;
        attempt.SkippedCount = skipped;
        attempt.CorrectPercentage = answers.Count == 0 ? 0 : decimal.Round(correct * 100m / answers.Count, 2);
    }
}
