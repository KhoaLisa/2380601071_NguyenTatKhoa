using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NguyenTatKhoa_2380601071.Migrations
{
    /// <inheritdoc />
    public partial class ToeicReadingDomain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ToeicExams",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    DurationMinutes = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    IsOfficialFormat = table.Column<bool>(type: "bit", nullable: false),
                    IsLocked = table.Column<bool>(type: "bit", nullable: false),
                    PublishedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorUserId = table.Column<long>(type: "bigint", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierUserId = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeleterUserId = table.Column<long>(type: "bigint", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ToeicExams", x => x.Id);
                    table.CheckConstraint("CK_ToeicExams_DurationMinutes", "[DurationMinutes] > 0");
                });

            migrationBuilder.CreateTable(
                name: "ToeicExamAttempts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExamId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DueAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SubmittedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    TotalQuestions = table.Column<int>(type: "int", nullable: false),
                    CorrectCount = table.Column<int>(type: "int", nullable: false),
                    IncorrectCount = table.Column<int>(type: "int", nullable: false),
                    SkippedCount = table.Column<int>(type: "int", nullable: false),
                    CorrectPercentage = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ToeicExamAttempts", x => x.Id);
                    table.CheckConstraint("CK_ToeicExamAttempts_Counts", "[CorrectCount] >= 0 AND [IncorrectCount] >= 0 AND [SkippedCount] >= 0 AND [TotalQuestions] >= 0");
                    table.ForeignKey(
                        name: "FK_ToeicExamAttempts_ToeicExams_ExamId",
                        column: x => x.ExamId,
                        principalTable: "ToeicExams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ToeicPassageGroups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExamId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Part = table.Column<int>(type: "int", nullable: false),
                    ExternalId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    AllowQuestionShuffle = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ToeicPassageGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ToeicPassageGroups_ToeicExams_ExamId",
                        column: x => x.ExamId,
                        principalTable: "ToeicExams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ToeicContentBlocks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PassageGroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentKey = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    TextContent = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BinaryData = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    MediaType = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    FileName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ToeicContentBlocks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ToeicContentBlocks_ToeicPassageGroups_PassageGroupId",
                        column: x => x.PassageGroupId,
                        principalTable: "ToeicPassageGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ToeicAttemptQuestions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AttemptId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuestionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    OptionOrderCsv = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SelectedOptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AnsweredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsCorrect = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ToeicAttemptQuestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ToeicAttemptQuestions_ToeicExamAttempts_AttemptId",
                        column: x => x.AttemptId,
                        principalTable: "ToeicExamAttempts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ToeicQuestionOptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuestionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Key = table.Column<string>(type: "nvarchar(1)", maxLength: 1, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    Text = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ToeicQuestionOptions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ToeicQuestions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExamId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PassageGroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Part = table.Column<int>(type: "int", nullable: false),
                    OriginalNumber = table.Column<int>(type: "int", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    Prompt = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: true),
                    Hint = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    AllowQuestionShuffle = table.Column<bool>(type: "bit", nullable: false),
                    AllowOptionShuffle = table.Column<bool>(type: "bit", nullable: false),
                    CorrectOptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ToeicQuestions", x => x.Id);
                    table.CheckConstraint("CK_ToeicQuestions_OriginalNumber", "[OriginalNumber] BETWEEN 101 AND 200");
                    table.ForeignKey(
                        name: "FK_ToeicQuestions_ToeicExams_ExamId",
                        column: x => x.ExamId,
                        principalTable: "ToeicExams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ToeicQuestions_ToeicPassageGroups_PassageGroupId",
                        column: x => x.PassageGroupId,
                        principalTable: "ToeicPassageGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ToeicQuestions_ToeicQuestionOptions_CorrectOptionId",
                        column: x => x.CorrectOptionId,
                        principalTable: "ToeicQuestionOptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ToeicAttemptQuestions_AttemptId_DisplayOrder",
                table: "ToeicAttemptQuestions",
                columns: new[] { "AttemptId", "DisplayOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ToeicAttemptQuestions_AttemptId_QuestionId",
                table: "ToeicAttemptQuestions",
                columns: new[] { "AttemptId", "QuestionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ToeicAttemptQuestions_QuestionId",
                table: "ToeicAttemptQuestions",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_ToeicAttemptQuestions_SelectedOptionId",
                table: "ToeicAttemptQuestions",
                column: "SelectedOptionId");

            migrationBuilder.CreateIndex(
                name: "IX_ToeicContentBlocks_PassageGroupId_DisplayOrder",
                table: "ToeicContentBlocks",
                columns: new[] { "PassageGroupId", "DisplayOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ToeicExamAttempts_ExamId_UserId_StartedAtUtc",
                table: "ToeicExamAttempts",
                columns: new[] { "ExamId", "UserId", "StartedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ToeicExams_Code",
                table: "ToeicExams",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ToeicPassageGroups_ExamId_DisplayOrder",
                table: "ToeicPassageGroups",
                columns: new[] { "ExamId", "DisplayOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ToeicPassageGroups_ExamId_ExternalId",
                table: "ToeicPassageGroups",
                columns: new[] { "ExamId", "ExternalId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ToeicQuestionOptions_QuestionId_DisplayOrder",
                table: "ToeicQuestionOptions",
                columns: new[] { "QuestionId", "DisplayOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ToeicQuestionOptions_QuestionId_Key",
                table: "ToeicQuestionOptions",
                columns: new[] { "QuestionId", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ToeicQuestions_CorrectOptionId",
                table: "ToeicQuestions",
                column: "CorrectOptionId");

            migrationBuilder.CreateIndex(
                name: "IX_ToeicQuestions_ExamId_DisplayOrder",
                table: "ToeicQuestions",
                columns: new[] { "ExamId", "DisplayOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ToeicQuestions_ExamId_OriginalNumber",
                table: "ToeicQuestions",
                columns: new[] { "ExamId", "OriginalNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ToeicQuestions_PassageGroupId",
                table: "ToeicQuestions",
                column: "PassageGroupId");

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
                name: "FK_ToeicQuestionOptions_ToeicQuestions_QuestionId",
                table: "ToeicQuestionOptions",
                column: "QuestionId",
                principalTable: "ToeicQuestions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ToeicQuestions_ToeicQuestionOptions_CorrectOptionId",
                table: "ToeicQuestions");

            migrationBuilder.DropTable(
                name: "ToeicAttemptQuestions");

            migrationBuilder.DropTable(
                name: "ToeicContentBlocks");

            migrationBuilder.DropTable(
                name: "ToeicExamAttempts");

            migrationBuilder.DropTable(
                name: "ToeicQuestionOptions");

            migrationBuilder.DropTable(
                name: "ToeicQuestions");

            migrationBuilder.DropTable(
                name: "ToeicPassageGroups");

            migrationBuilder.DropTable(
                name: "ToeicExams");
        }
    }
}
