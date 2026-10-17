using System;
using System.Collections.Generic;
using System.Linq;

namespace NguyenTatKhoa_2380601071.Toeic;

/// <summary>Copies content only; attempts, results, audit fields and publication state never cross versions.</summary>
public static class ExamDraftFactory
{
    public static Exam Create(Exam source, string code, string title)
    {
        var copy = new Exam
        {
            Id = Guid.NewGuid(), Code = code, Title = title,
            SourceExamId = source.SourceExamId, TagVersion = source.TagVersion,
            Directions = source.Directions, DeclaredQuestionCount = source.DeclaredQuestionCount,
            SourceFileName = source.SourceFileName, SourceHash = source.SourceHash,
            DurationMinutes = source.DurationMinutes, IsOfficialFormat = source.IsOfficialFormat,
            Status = ExamStatus.Draft, Version = 1, IsLocked = false, PublishedAtUtc = null
        };
        var parts = new Dictionary<Guid, Guid>();
        foreach (var oldPart in source.Parts.OrderBy(x => x.DisplayOrder))
        {
            var part = new ExamPart
            {
                Id = Guid.NewGuid(), ExamId = copy.Id, PartNumber = oldPart.PartNumber,
                DisplayOrder = oldPart.DisplayOrder, Directions = oldPart.Directions,
                AllowShuffleGroups = oldPart.AllowShuffleGroups
            };
            parts[oldPart.Id] = part.Id;
            copy.Parts.Add(part);
        }
        var groups = new Dictionary<Guid, Guid>();
        foreach (var oldGroup in source.PassageGroups.OrderBy(x => x.DisplayOrder))
        {
            var group = new PassageGroup
            {
                Id = Guid.NewGuid(), ExamId = copy.Id, ExamPartId = parts[oldGroup.ExamPartId],
                Part = oldGroup.Part, ExternalId = oldGroup.ExternalId, Title = oldGroup.Title,
                DisplayOrder = oldGroup.DisplayOrder, FirstQuestionNumber = oldGroup.FirstQuestionNumber,
                LastQuestionNumber = oldGroup.LastQuestionNumber,
                AllowQuestionShuffle = oldGroup.AllowQuestionShuffle
            };
            var passages = new Dictionary<Guid, Guid>();
            foreach (var oldPassage in oldGroup.Passages.OrderBy(x => x.DisplayOrder))
            {
                var passage = new Passage
                {
                    Id = Guid.NewGuid(), PassageGroupId = group.Id,
                    SourcePassageId = oldPassage.SourcePassageId, Kind = oldPassage.Kind,
                    DisplayOrder = oldPassage.DisplayOrder, BlankQuestionNumbers = oldPassage.BlankQuestionNumbers
                };
                passages[oldPassage.Id] = passage.Id;
                group.Passages.Add(passage);
            }
            foreach (var oldBlock in oldGroup.ContentBlocks.OrderBy(x => x.DisplayOrder))
            {
                group.ContentBlocks.Add(new ContentBlock
                {
                    Id = Guid.NewGuid(), PassageGroupId = group.Id, PassageId = passages[oldBlock.PassageId],
                    SourceBlockId = oldBlock.SourceBlockId, DocumentKey = oldBlock.DocumentKey,
                    DisplayOrder = oldBlock.DisplayOrder, Type = oldBlock.Type, TextContent = oldBlock.TextContent,
                    BinaryData = oldBlock.BinaryData?.ToArray(), MediaType = oldBlock.MediaType,
                    FileName = oldBlock.FileName, SourcePage = oldBlock.SourcePage
                });
            }
            groups[oldGroup.Id] = group.Id;
            copy.PassageGroups.Add(group);
        }
        foreach (var oldQuestion in source.Questions.OrderBy(x => x.DisplayOrder))
        {
            var question = new Question
            {
                Id = Guid.NewGuid(), ExamId = copy.Id, ExamPartId = parts[oldQuestion.ExamPartId],
                SourceQuestionId = oldQuestion.SourceQuestionId, Part = oldQuestion.Part,
                PassageGroupId = oldQuestion.PassageGroupId.HasValue ? groups[oldQuestion.PassageGroupId.Value] : null,
                OriginalNumber = oldQuestion.OriginalNumber, DisplayOrder = oldQuestion.DisplayOrder,
                Prompt = oldQuestion.Prompt, Hint = oldQuestion.Hint,
                AllowQuestionShuffle = oldQuestion.AllowQuestionShuffle, AllowOptionShuffle = oldQuestion.AllowOptionShuffle
            };
            foreach (var oldOption in oldQuestion.Options.OrderBy(x => x.DisplayOrder))
            {
                var option = new QuestionOption
                {
                    Id = Guid.NewGuid(), QuestionId = question.Id, Key = oldOption.Key,
                    Text = oldOption.Text, DisplayOrder = oldOption.DisplayOrder,
                    IsCorrect = oldOption.IsCorrect
                };
                question.Options.Add(option);
                if (oldOption.Id == oldQuestion.CorrectOptionId) question.CorrectOptionId = option.Id;
            }
            copy.Questions.Add(question);
        }
        return copy;
    }
}
