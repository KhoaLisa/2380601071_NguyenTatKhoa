-- Read-only. In SSMS select the application database first (never master).
SET NOCOUNT ON;
IF DB_NAME() IN ('master','model','msdb','tempdb')
    THROW 51020,'Select the TOEIC application database before running verification.',1;
SELECT CAST(@@SERVERNAME AS nvarchar(128)) AS ServerName, DB_NAME() AS DatabaseName,
    CAST(ORIGINAL_LOGIN() AS nvarchar(128)) AS OriginalLogin, SYSUTCDATETIME() AS ServerUtc;
SELECT MigrationId, ProductVersion FROM dbo.__EFMigrationsHistory ORDER BY MigrationId;
SELECT t.name AS BusinessTable, (SELECT COUNT(*) FROM sys.columns c WHERE c.object_id=t.object_id) AS ColumnCount
FROM sys.tables t WHERE t.name LIKE 'Toeic%' ORDER BY t.name;
SELECT OBJECT_NAME(parent_object_id) AS TableName, name AS ForeignKey, is_disabled, is_not_trusted
FROM sys.foreign_keys WHERE OBJECT_NAME(parent_object_id) LIKE 'Toeic%' ORDER BY TableName,name;
SELECT OBJECT_NAME(object_id) AS TableName,name,is_unique,filter_definition
FROM sys.indexes WHERE OBJECT_NAME(object_id) LIKE 'Toeic%' AND name IS NOT NULL ORDER BY TableName,name;
SELECT OBJECT_NAME(parent_object_id) AS TableName,name,definition,is_disabled,is_not_trusted
FROM sys.check_constraints WHERE OBJECT_NAME(parent_object_id) LIKE 'Toeic%' ORDER BY TableName,name;
SELECT (SELECT COUNT(*) FROM ToeicExams) AS Exams,
       (SELECT COUNT(*) FROM ToeicExamParts) AS Parts,
       (SELECT COUNT(*) FROM ToeicPassageGroups) AS Groups,
       (SELECT COUNT(*) FROM ToeicPassages) AS Passages,
       (SELECT COUNT(*) FROM ToeicContentBlocks WHERE Type=2) AS Images,
       (SELECT COUNT(*) FROM ToeicQuestions) AS Questions,
       (SELECT COUNT(*) FROM ToeicQuestionOptions) AS Choices,
       (SELECT COUNT(*) FROM ToeicExamAttempts) AS Attempts;
-- Every following data-integrity query must return zero rows.
SELECT q.Id,q.OriginalNumber FROM ToeicQuestions q LEFT JOIN ToeicQuestionOptions o ON o.QuestionId=q.Id
GROUP BY q.Id,q.OriginalNumber HAVING COUNT(o.Id)<>4 OR SUM(CASE WHEN o.IsCorrect=1 THEN 1 ELSE 0 END)<>1;
SELECT q.Id,q.OriginalNumber FROM ToeicQuestions q
WHERE (q.Part=5 AND q.PassageGroupId IS NOT NULL) OR (q.Part IN (6,7) AND q.PassageGroupId IS NULL);
SELECT a.Id FROM ToeicAttemptQuestions a JOIN ToeicQuestionOptions o ON o.Id=a.SelectedOptionId WHERE o.QuestionId<>a.QuestionId;
SELECT a.Id FROM ToeicExamAttempts a WHERE Status IN (1,2) AND CorrectCount+IncorrectCount+SkippedCount<>TotalQuestions;
SELECT b.Id FROM ToeicContentBlocks b WHERE Type=2 AND (DATALENGTH(BinaryData)=0 OR BinaryData IS NULL OR MediaType NOT LIKE 'image/%');
