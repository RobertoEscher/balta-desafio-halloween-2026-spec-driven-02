using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Shortener.Api.Data;

#nullable disable

namespace Shortener.Api.Data.Migrations;

[DbContext(typeof(LinkDbContext))]
[Migration("20261026190000_InitialCreate")]
public sealed class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Links",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Code = table.Column<string>(type: "TEXT", maxLength: 7, nullable: false),
                TargetUrl = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: false),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                ExpiresAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                AccessCount = table.Column<long>(type: "INTEGER", nullable: false, defaultValue: 0L)
            },
            constraints: table => table.PrimaryKey("PK_Links", item => item.Id));

        migrationBuilder.CreateIndex(
            name: "IX_Links_Code",
            table: "Links",
            column: "Code",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropTable(name: "Links");
}
