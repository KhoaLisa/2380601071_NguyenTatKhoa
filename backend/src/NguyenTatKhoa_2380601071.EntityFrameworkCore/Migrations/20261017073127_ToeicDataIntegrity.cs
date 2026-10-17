using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NguyenTatKhoa_2380601071.Migrations
{
    /// <inheritdoc />
    public partial class ToeicDataIntegrity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ToeicAttemptQuestions_ToeicExamAttempts_AttemptId",
                table: "ToeicAttemptQuestions");

            migrationBuilder.DropForeignKey(
                name: "FK_ToeicAttemptQuestions_ToeicQuestionOptions_SelectedOptionId",
                table: "ToeicAttemptQuestions");

            migrationBuilder.DropForeignKey(
                name: "FK_ToeicAttemptQuestions_ToeicQuestions_QuestionId",
                table: "ToeicAttemptQuestions");

            migrationBuilder.DropForeignKey(
                name: "FK_ToeicQuestions_ToeicPassageGroups_PassageGroupId",
                table: "ToeicQuestions");

            migrationBuilder.DropForeignKey(
                name: "FK_ToeicQuestions_ToeicQuestionOptions_CorrectOptionId",
                table: "ToeicQuestions");

            migrationBuilder.DropIndex(
                name: "IX_ToeicQuestions_CorrectOptionId",
                table: "ToeicQuestions");

            migrationBuilder.DropIndex(
                name: "IX_ToeicQuestions_PassageGroupId",
                table: "ToeicQuestions");

            migrationBuilder.DropIndex(
                name: "IX_ToeicAttemptQuestions_QuestionId",
                table: "ToeicAttemptQuestions");

            migrationBuilder.DropIndex(
                name: "IX_ToeicAttemptQuestions_SelectedOptionId",
                table: "ToeicAttemptQuestions");

            migrationBuilder.AddColumn<Guid>(
                name: "ExamPartId",
                table: "ToeicQuestions",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "SourceQuestionId",
                table: "ToeicQuestions",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsCorrect",
                table: "ToeicQuestionOptions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "ExamPartId",
                table: "ToeicPassageGroups",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<int>(
                name: "FirstQuestionNumber",
                table: "ToeicPassageGroups",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "LastQuestionNumber",
                table: "ToeicPassageGroups",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DeclaredQuestionCount",
                table: "ToeicExams",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Directions",
                table: "ToeicExams",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "ToeicExams",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceExamId",
                table: "ToeicExams",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SourceFileName",
                table: "ToeicExams",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceHash",
                table: "ToeicExams",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TagVersion",
                table: "ToeicExams",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "DurationMinutesSnapshot",
                table: "ToeicExamAttempts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "PassageId",
                table: "ToeicContentBlocks",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "SourceBlockId",
                table: "ToeicContentBlocks",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "SourcePage",
                table: "ToeicContentBlocks",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ExamId",
                table: "ToeicAttemptQuestions",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddUniqueConstraint(
                name: "AK_ToeicQuestions_Id_ExamId",
                table: "ToeicQuestions",
                columns: new[] { "Id", "ExamId" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_ToeicQuestionOptions_Id_QuestionId",
                table: "ToeicQuestionOptions",
                columns: new[] { "Id", "QuestionId" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_ToeicPassageGroups_Id_ExamId_ExamPartId",
                table: "ToeicPassageGroups",
                columns: new[] { "Id", "ExamId", "ExamPartId" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_ToeicExamAttempts_Id_ExamId",
                table: "ToeicExamAttempts",
                columns: new[] { "Id", "ExamId" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_ToeicAttemptQuestions_Id_QuestionId",
                table: "ToeicAttemptQuestions",
                columns: new[] { "Id", "QuestionId" });

            migrationBuilder.CreateTable(
                name: "ToeicAttemptChoices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AttemptQuestionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuestionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChoiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ToeicAttemptChoices", x => x.Id);
                    table.CheckConstraint("CK_ToeicAttemptChoices_Order", "[DisplayOrder] BETWEEN 1 AND 4");
                    table.ForeignKey(
                        name: "FK_ToeicAttemptChoices_ToeicAttemptQuestions_AttemptQuestionId_QuestionId",
                        columns: x => new { x.AttemptQuestionId, x.QuestionId },
                        principalTable: "ToeicAttemptQuestions",
                        principalColumns: new[] { "Id", "QuestionId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ToeicAttemptChoices_ToeicQuestionOptions_ChoiceId_QuestionId",
                        columns: x => new { x.ChoiceId, x.QuestionId },
                        principalTable: "ToeicQuestionOptions",
                        principalColumns: new[] { "Id", "QuestionId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ToeicExamParts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExamId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PartNumber = table.Column<int>(type: "int", nullable: false),
                    Directions = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    AllowShuffleGroups = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ToeicExamParts", x => x.Id);
                    table.UniqueConstraint("AK_ToeicExamParts_Id_ExamId_PartNumber", x => new { x.Id, x.ExamId, x.PartNumber });
                    table.CheckConstraint("CK_ToeicExamParts_Values", "[PartNumber] IN (5,6,7) AND [DisplayOrder] > 0");
                    table.ForeignKey(
                        name: "FK_ToeicExamParts_ToeicExams_ExamId",
                        column: x => x.ExamId,
                        principalTable: "ToeicExams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ToeicPassages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PassageGroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourcePassageId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    BlankQuestionNumbers = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ToeicPassages", x => x.Id);
                    table.UniqueConstraint("AK_ToeicPassages_Id_PassageGroupId", x => new { x.Id, x.PassageGroupId });
                    table.CheckConstraint("CK_ToeicPassages_Order", "[DisplayOrder] > 0");
                    table.ForeignKey(
                        name: "FK_ToeicPassages_ToeicPassageGroups_PassageGroupId",
                        column: x => x.PassageGroupId,
                        principalTable: "ToeicPassageGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            // Preserve existing TOEIC data before adding the stronger FKs and UNIQUEs.
            // No table, row or migration history is discarded by this forward migration.
            migrationBuilder.Sql(@"
UPDATE e SET SourceExamId=e.Code, TagVersion='1.0',
    DeclaredQuestionCount=(SELECT COUNT(*) FROM ToeicQuestions q WHERE q.ExamId=e.Id)
FROM ToeicExams e;
INSERT INTO ToeicExamParts(Id,ExamId,PartNumber,DisplayOrder,AllowShuffleGroups)
SELECT NEWID(),e.Id,p.PartNumber,p.PartNumber-4,0
FROM ToeicExams e CROSS JOIN (VALUES(5),(6),(7))p(PartNumber);
UPDATE q SET ExamPartId=p.Id, SourceQuestionId=CONCAT('q',q.OriginalNumber)
FROM ToeicQuestions q JOIN ToeicExamParts p ON p.ExamId=q.ExamId AND p.PartNumber=q.Part;
UPDATE g SET ExamPartId=p.Id,
    FirstQuestionNumber=COALESCE((SELECT MIN(q.OriginalNumber) FROM ToeicQuestions q WHERE q.PassageGroupId=g.Id),0),
    LastQuestionNumber=COALESCE((SELECT MAX(q.OriginalNumber) FROM ToeicQuestions q WHERE q.PassageGroupId=g.Id),0)
FROM ToeicPassageGroups g JOIN ToeicExamParts p ON p.ExamId=g.ExamId AND p.PartNumber=g.Part;
INSERT INTO ToeicPassages(Id,PassageGroupId,SourcePassageId,Kind,DisplayOrder,BlankQuestionNumbers)
SELECT NEWID(),PassageGroupId,DocumentKey,'legacy-document',
    ROW_NUMBER() OVER(PARTITION BY PassageGroupId ORDER BY MIN(DisplayOrder)), '[]'
FROM ToeicContentBlocks GROUP BY PassageGroupId,DocumentKey;
UPDATE b SET PassageId=p.Id,SourceBlockId=CONCAT('block-',b.DisplayOrder)
FROM ToeicContentBlocks b JOIN ToeicPassages p ON p.PassageGroupId=b.PassageGroupId AND p.SourcePassageId=b.DocumentKey;
UPDATE o SET IsCorrect=1 FROM ToeicQuestionOptions o JOIN ToeicQuestions q ON q.CorrectOptionId=o.Id;
UPDATE a SET DurationMinutesSnapshot=e.DurationMinutes FROM ToeicExamAttempts a JOIN ToeicExams e ON e.Id=a.ExamId;
UPDATE a SET ExamId=q.ExamId FROM ToeicAttemptQuestions a JOIN ToeicQuestions q ON q.Id=a.QuestionId;
INSERT INTO ToeicAttemptChoices(Id,AttemptQuestionId,QuestionId,ChoiceId,DisplayOrder)
SELECT NEWID(),a.Id,a.QuestionId,o.Id,CONVERT(int,j.[key])+1
FROM ToeicAttemptQuestions a CROSS APPLY OPENJSON('[""'+REPLACE(a.OptionOrderCsv,',','"",""')+'""]')j
JOIN ToeicQuestionOptions o ON o.QuestionId=a.QuestionId AND REPLACE(CONVERT(varchar(36),o.Id),'-','')=j.value;

IF EXISTS(SELECT 1 FROM ToeicExamAttempts GROUP BY UserId,ExamId HAVING SUM(CASE WHEN Status=0 THEN 1 ELSE 0 END)>1)
    THROW 51002,'Existing duplicate active attempts require review. Migration cancelled without deleting them.',1;
");

            migrationBuilder.CreateIndex(
                name: "IX_ToeicQuestions_CorrectOptionId_Id",
                table: "ToeicQuestions",
                columns: new[] { "CorrectOptionId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_ToeicQuestions_ExamId_SourceQuestionId",
                table: "ToeicQuestions",
                columns: new[] { "ExamId", "SourceQuestionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ToeicQuestions_ExamPartId_ExamId_Part",
                table: "ToeicQuestions",
                columns: new[] { "ExamPartId", "ExamId", "Part" });

            migrationBuilder.CreateIndex(
                name: "IX_ToeicQuestions_PassageGroupId_ExamId_ExamPartId",
                table: "ToeicQuestions",
                columns: new[] { "PassageGroupId", "ExamId", "ExamPartId" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_ToeicQuestions_PartAndGroup",
                table: "ToeicQuestions",
                sql: "[DisplayOrder]>0 AND (([Part]=5 AND [OriginalNumber] BETWEEN 101 AND 130 AND [PassageGroupId] IS NULL) OR ([Part]=6 AND [OriginalNumber] BETWEEN 131 AND 146 AND [PassageGroupId] IS NOT NULL) OR ([Part]=7 AND [OriginalNumber] BETWEEN 147 AND 200 AND [PassageGroupId] IS NOT NULL))");

            migrationBuilder.CreateIndex(
                name: "UX_ToeicQuestionOptions_OneCorrect",
                table: "ToeicQuestionOptions",
                column: "QuestionId",
                unique: true,
                filter: "[IsCorrect] = 1");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ToeicQuestionOptions_Values",
                table: "ToeicQuestionOptions",
                sql: "[Key] IN ('A','B','C','D') AND [DisplayOrder]>0");

            migrationBuilder.CreateIndex(
                name: "IX_ToeicPassageGroups_ExamPartId_ExamId_Part",
                table: "ToeicPassageGroups",
                columns: new[] { "ExamPartId", "ExamId", "Part" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_ToeicPassageGroups_Values",
                table: "ToeicPassageGroups",
                sql: "[DisplayOrder] > 0 AND [Part] IN (6,7) AND [FirstQuestionNumber] <= [LastQuestionNumber]");

            migrationBuilder.CreateIndex(
                name: "IX_ToeicExams_SourceHash",
                table: "ToeicExams",
                column: "SourceHash");

            migrationBuilder.CreateIndex(
                name: "IX_ToeicExams_Status_PublishedAtUtc",
                table: "ToeicExams",
                columns: new[] { "Status", "PublishedAtUtc" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_ToeicExams_Status",
                table: "ToeicExams",
                sql: "[Status] IN (0,1,2)");

            migrationBuilder.CreateIndex(
                name: "IX_ToeicExamAttempts_Status_DueAtUtc",
                table: "ToeicExamAttempts",
                columns: new[] { "Status", "DueAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ToeicExamAttempts_UserId_ExamId",
                table: "ToeicExamAttempts",
                columns: new[] { "UserId", "ExamId" },
                unique: true,
                filter: "[Status]=0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ToeicExamAttempts_FinalResult",
                table: "ToeicExamAttempts",
                sql: "[Status] IN (0,1,2) AND [DurationMinutesSnapshot]>0 AND [DueAtUtc]>=[StartedAtUtc] AND [CorrectPercentage] BETWEEN 0 AND 100 AND (([Status]=0 AND [SubmittedAtUtc] IS NULL) OR ([Status] IN (1,2) AND [SubmittedAtUtc] IS NOT NULL AND [CorrectCount]+[IncorrectCount]+[SkippedCount]=[TotalQuestions]))");

            migrationBuilder.CreateIndex(
                name: "IX_ToeicContentBlocks_PassageId_DisplayOrder",
                table: "ToeicContentBlocks",
                columns: new[] { "PassageId", "DisplayOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ToeicContentBlocks_PassageId_PassageGroupId",
                table: "ToeicContentBlocks",
                columns: new[] { "PassageId", "PassageGroupId" });

            migrationBuilder.CreateIndex(
                name: "IX_ToeicContentBlocks_PassageId_SourceBlockId",
                table: "ToeicContentBlocks",
                columns: new[] { "PassageId", "SourceBlockId" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_ToeicContentBlocks_Content",
                table: "ToeicContentBlocks",
                sql: "([Type]=2 AND DATALENGTH([BinaryData])>0 AND [MediaType] LIKE 'image/%' AND [TextContent] IS NULL) OR ([Type] IN (1,3) AND [TextContent] IS NOT NULL AND [BinaryData] IS NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ToeicContentBlocks_Order",
                table: "ToeicContentBlocks",
                sql: "[DisplayOrder] > 0");

            migrationBuilder.CreateIndex(
                name: "IX_ToeicAttemptQuestions_AttemptId_ExamId",
                table: "ToeicAttemptQuestions",
                columns: new[] { "AttemptId", "ExamId" });

            migrationBuilder.CreateIndex(
                name: "IX_ToeicAttemptQuestions_QuestionId_ExamId",
                table: "ToeicAttemptQuestions",
                columns: new[] { "QuestionId", "ExamId" });

            migrationBuilder.CreateIndex(
                name: "IX_ToeicAttemptQuestions_SelectedOptionId_QuestionId",
                table: "ToeicAttemptQuestions",
                columns: new[] { "SelectedOptionId", "QuestionId" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_ToeicAttemptQuestions_Order",
                table: "ToeicAttemptQuestions",
                sql: "[DisplayOrder]>0");

            migrationBuilder.CreateIndex(
                name: "IX_ToeicAttemptChoices_AttemptQuestionId_ChoiceId",
                table: "ToeicAttemptChoices",
                columns: new[] { "AttemptQuestionId", "ChoiceId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ToeicAttemptChoices_AttemptQuestionId_DisplayOrder",
                table: "ToeicAttemptChoices",
                columns: new[] { "AttemptQuestionId", "DisplayOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ToeicAttemptChoices_AttemptQuestionId_QuestionId",
                table: "ToeicAttemptChoices",
                columns: new[] { "AttemptQuestionId", "QuestionId" });

            migrationBuilder.CreateIndex(
                name: "IX_ToeicAttemptChoices_ChoiceId_QuestionId",
                table: "ToeicAttemptChoices",
                columns: new[] { "ChoiceId", "QuestionId" });

            migrationBuilder.CreateIndex(
                name: "IX_ToeicExamParts_ExamId_DisplayOrder",
                table: "ToeicExamParts",
                columns: new[] { "ExamId", "DisplayOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ToeicExamParts_ExamId_PartNumber",
                table: "ToeicExamParts",
                columns: new[] { "ExamId", "PartNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ToeicPassages_PassageGroupId_DisplayOrder",
                table: "ToeicPassages",
                columns: new[] { "PassageGroupId", "DisplayOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ToeicPassages_PassageGroupId_SourcePassageId",
                table: "ToeicPassages",
                columns: new[] { "PassageGroupId", "SourcePassageId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ToeicAttemptQuestions_ToeicExamAttempts_AttemptId_ExamId",
                table: "ToeicAttemptQuestions",
                columns: new[] { "AttemptId", "ExamId" },
                principalTable: "ToeicExamAttempts",
                principalColumns: new[] { "Id", "ExamId" },
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ToeicAttemptQuestions_ToeicQuestionOptions_SelectedOptionId_QuestionId",
                table: "ToeicAttemptQuestions",
                columns: new[] { "SelectedOptionId", "QuestionId" },
                principalTable: "ToeicQuestionOptions",
                principalColumns: new[] { "Id", "QuestionId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ToeicAttemptQuestions_ToeicQuestions_QuestionId_ExamId",
                table: "ToeicAttemptQuestions",
                columns: new[] { "QuestionId", "ExamId" },
                principalTable: "ToeicQuestions",
                principalColumns: new[] { "Id", "ExamId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ToeicContentBlocks_ToeicPassages_PassageId_PassageGroupId",
                table: "ToeicContentBlocks",
                columns: new[] { "PassageId", "PassageGroupId" },
                principalTable: "ToeicPassages",
                principalColumns: new[] { "Id", "PassageGroupId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ToeicExamAttempts_AbpUsers_UserId",
                table: "ToeicExamAttempts",
                column: "UserId",
                principalTable: "AbpUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ToeicPassageGroups_ToeicExamParts_ExamPartId_ExamId_Part",
                table: "ToeicPassageGroups",
                columns: new[] { "ExamPartId", "ExamId", "Part" },
                principalTable: "ToeicExamParts",
                principalColumns: new[] { "Id", "ExamId", "PartNumber" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ToeicQuestions_ToeicExamParts_ExamPartId_ExamId_Part",
                table: "ToeicQuestions",
                columns: new[] { "ExamPartId", "ExamId", "Part" },
                principalTable: "ToeicExamParts",
                principalColumns: new[] { "Id", "ExamId", "PartNumber" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ToeicQuestions_ToeicPassageGroups_PassageGroupId_ExamId_ExamPartId",
                table: "ToeicQuestions",
                columns: new[] { "PassageGroupId", "ExamId", "ExamPartId" },
                principalTable: "ToeicPassageGroups",
                principalColumns: new[] { "Id", "ExamId", "ExamPartId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ToeicQuestions_ToeicQuestionOptions_CorrectOptionId_Id",
                table: "ToeicQuestions",
                columns: new[] { "CorrectOptionId", "Id" },
                principalTable: "ToeicQuestionOptions",
                principalColumns: new[] { "Id", "QuestionId" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ToeicAttemptQuestions_ToeicExamAttempts_AttemptId_ExamId",
                table: "ToeicAttemptQuestions");

            migrationBuilder.DropForeignKey(
                name: "FK_ToeicAttemptQuestions_ToeicQuestionOptions_SelectedOptionId_QuestionId",
                table: "ToeicAttemptQuestions");

            migrationBuilder.DropForeignKey(
                name: "FK_ToeicAttemptQuestions_ToeicQuestions_QuestionId_ExamId",
                table: "ToeicAttemptQuestions");

            migrationBuilder.DropForeignKey(
                name: "FK_ToeicContentBlocks_ToeicPassages_PassageId_PassageGroupId",
                table: "ToeicContentBlocks");

            migrationBuilder.DropForeignKey(
                name: "FK_ToeicExamAttempts_AbpUsers_UserId",
                table: "ToeicExamAttempts");

            migrationBuilder.DropForeignKey(
                name: "FK_ToeicPassageGroups_ToeicExamParts_ExamPartId_ExamId_Part",
                table: "ToeicPassageGroups");

            migrationBuilder.DropForeignKey(
                name: "FK_ToeicQuestions_ToeicExamParts_ExamPartId_ExamId_Part",
                table: "ToeicQuestions");

            migrationBuilder.DropForeignKey(
                name: "FK_ToeicQuestions_ToeicPassageGroups_PassageGroupId_ExamId_ExamPartId",
                table: "ToeicQuestions");

            migrationBuilder.DropForeignKey(
                name: "FK_ToeicQuestions_ToeicQuestionOptions_CorrectOptionId_Id",
                table: "ToeicQuestions");

            migrationBuilder.DropTable(
                name: "ToeicAttemptChoices");

            migrationBuilder.DropTable(
                name: "ToeicExamParts");

            migrationBuilder.DropTable(
                name: "ToeicPassages");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_ToeicQuestions_Id_ExamId",
                table: "ToeicQuestions");

            migrationBuilder.DropIndex(
                name: "IX_ToeicQuestions_CorrectOptionId_Id",
                table: "ToeicQuestions");

            migrationBuilder.DropIndex(
                name: "IX_ToeicQuestions_ExamId_SourceQuestionId",
                table: "ToeicQuestions");

            migrationBuilder.DropIndex(
                name: "IX_ToeicQuestions_ExamPartId_ExamId_Part",
                table: "ToeicQuestions");

            migrationBuilder.DropIndex(
                name: "IX_ToeicQuestions_PassageGroupId_ExamId_ExamPartId",
                table: "ToeicQuestions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ToeicQuestions_PartAndGroup",
                table: "ToeicQuestions");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_ToeicQuestionOptions_Id_QuestionId",
                table: "ToeicQuestionOptions");

            migrationBuilder.DropIndex(
                name: "UX_ToeicQuestionOptions_OneCorrect",
                table: "ToeicQuestionOptions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ToeicQuestionOptions_Values",
                table: "ToeicQuestionOptions");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_ToeicPassageGroups_Id_ExamId_ExamPartId",
                table: "ToeicPassageGroups");

            migrationBuilder.DropIndex(
                name: "IX_ToeicPassageGroups_ExamPartId_ExamId_Part",
                table: "ToeicPassageGroups");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ToeicPassageGroups_Values",
                table: "ToeicPassageGroups");

            migrationBuilder.DropIndex(
                name: "IX_ToeicExams_SourceHash",
                table: "ToeicExams");

            migrationBuilder.DropIndex(
                name: "IX_ToeicExams_Status_PublishedAtUtc",
                table: "ToeicExams");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ToeicExams_Status",
                table: "ToeicExams");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_ToeicExamAttempts_Id_ExamId",
                table: "ToeicExamAttempts");

            migrationBuilder.DropIndex(
                name: "IX_ToeicExamAttempts_Status_DueAtUtc",
                table: "ToeicExamAttempts");

            migrationBuilder.DropIndex(
                name: "IX_ToeicExamAttempts_UserId_ExamId",
                table: "ToeicExamAttempts");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ToeicExamAttempts_FinalResult",
                table: "ToeicExamAttempts");

            migrationBuilder.DropIndex(
                name: "IX_ToeicContentBlocks_PassageId_DisplayOrder",
                table: "ToeicContentBlocks");

            migrationBuilder.DropIndex(
                name: "IX_ToeicContentBlocks_PassageId_PassageGroupId",
                table: "ToeicContentBlocks");

            migrationBuilder.DropIndex(
                name: "IX_ToeicContentBlocks_PassageId_SourceBlockId",
                table: "ToeicContentBlocks");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ToeicContentBlocks_Content",
                table: "ToeicContentBlocks");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ToeicContentBlocks_Order",
                table: "ToeicContentBlocks");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_ToeicAttemptQuestions_Id_QuestionId",
                table: "ToeicAttemptQuestions");

            migrationBuilder.DropIndex(
                name: "IX_ToeicAttemptQuestions_AttemptId_ExamId",
                table: "ToeicAttemptQuestions");

            migrationBuilder.DropIndex(
                name: "IX_ToeicAttemptQuestions_QuestionId_ExamId",
                table: "ToeicAttemptQuestions");

            migrationBuilder.DropIndex(
                name: "IX_ToeicAttemptQuestions_SelectedOptionId_QuestionId",
                table: "ToeicAttemptQuestions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ToeicAttemptQuestions_Order",
                table: "ToeicAttemptQuestions");

            migrationBuilder.DropColumn(
                name: "ExamPartId",
                table: "ToeicQuestions");

            migrationBuilder.DropColumn(
                name: "SourceQuestionId",
                table: "ToeicQuestions");

            migrationBuilder.DropColumn(
                name: "IsCorrect",
                table: "ToeicQuestionOptions");

            migrationBuilder.DropColumn(
                name: "ExamPartId",
                table: "ToeicPassageGroups");

            migrationBuilder.DropColumn(
                name: "FirstQuestionNumber",
                table: "ToeicPassageGroups");

            migrationBuilder.DropColumn(
                name: "LastQuestionNumber",
                table: "ToeicPassageGroups");

            migrationBuilder.DropColumn(
                name: "DeclaredQuestionCount",
                table: "ToeicExams");

            migrationBuilder.DropColumn(
                name: "Directions",
                table: "ToeicExams");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "ToeicExams");

            migrationBuilder.DropColumn(
                name: "SourceExamId",
                table: "ToeicExams");

            migrationBuilder.DropColumn(
                name: "SourceFileName",
                table: "ToeicExams");

            migrationBuilder.DropColumn(
                name: "SourceHash",
                table: "ToeicExams");

            migrationBuilder.DropColumn(
                name: "TagVersion",
                table: "ToeicExams");

            migrationBuilder.DropColumn(
                name: "DurationMinutesSnapshot",
                table: "ToeicExamAttempts");

            migrationBuilder.DropColumn(
                name: "PassageId",
                table: "ToeicContentBlocks");

            migrationBuilder.DropColumn(
                name: "SourceBlockId",
                table: "ToeicContentBlocks");

            migrationBuilder.DropColumn(
                name: "SourcePage",
                table: "ToeicContentBlocks");

            migrationBuilder.DropColumn(
                name: "ExamId",
                table: "ToeicAttemptQuestions");

            migrationBuilder.CreateIndex(
                name: "IX_ToeicQuestions_CorrectOptionId",
                table: "ToeicQuestions",
                column: "CorrectOptionId");

            migrationBuilder.CreateIndex(
                name: "IX_ToeicQuestions_PassageGroupId",
                table: "ToeicQuestions",
                column: "PassageGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_ToeicAttemptQuestions_QuestionId",
                table: "ToeicAttemptQuestions",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_ToeicAttemptQuestions_SelectedOptionId",
                table: "ToeicAttemptQuestions",
                column: "SelectedOptionId");

            migrationBuilder.AddForeignKey(
                name: "FK_ToeicAttemptQuestions_ToeicExamAttempts_AttemptId",
                table: "ToeicAttemptQuestions",
                column: "AttemptId",
                principalTable: "ToeicExamAttempts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ToeicAttemptQuestions_ToeicQuestionOptions_SelectedOptionId",
                table: "ToeicAttemptQuestions",
                column: "SelectedOptionId",
                principalTable: "ToeicQuestionOptions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ToeicAttemptQuestions_ToeicQuestions_QuestionId",
                table: "ToeicAttemptQuestions",
                column: "QuestionId",
                principalTable: "ToeicQuestions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ToeicQuestions_ToeicPassageGroups_PassageGroupId",
                table: "ToeicQuestions",
                column: "PassageGroupId",
                principalTable: "ToeicPassageGroups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ToeicQuestions_ToeicQuestionOptions_CorrectOptionId",
                table: "ToeicQuestions",
                column: "CorrectOptionId",
                principalTable: "ToeicQuestionOptions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
