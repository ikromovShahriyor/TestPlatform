using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TestPlatform.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateTestAttemptColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE ""TestAttempts"" DROP COLUMN IF EXISTS ""StudentName"";
                ALTER TABLE ""TestAttempts"" DROP COLUMN IF EXISTS ""CorrectAnswersCount"";
                ALTER TABLE ""TestAttempts"" DROP COLUMN IF EXISTS ""TotalQuestions"";
                ALTER TABLE ""TestAttempts"" DROP COLUMN IF EXISTS ""Score"";
                ALTER TABLE ""TestAttempts"" DROP COLUMN IF EXISTS ""MaxScore"";
                ALTER TABLE ""TestAttempts"" DROP COLUMN IF EXISTS ""IsPassed"";
                ALTER TABLE ""TestAttempts"" DROP COLUMN IF EXISTS ""SubmittedAt"";

                ALTER TABLE ""TestAttempts"" ADD COLUMN IF NOT EXISTS ""TotalScore"" integer NOT NULL DEFAULT 0;
                ALTER TABLE ""TestAttempts"" ADD COLUMN IF NOT EXISTS ""EarnedScore"" integer NOT NULL DEFAULT 0;
                ALTER TABLE ""TestAttempts"" ADD COLUMN IF NOT EXISTS ""PassedAt"" timestamp with time zone NOT NULL DEFAULT NOW();
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
