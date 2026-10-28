using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MatForge.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPositionDisciplines : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PositionDisciplines",
                columns: table => new
                {
                    PositionId = table.Column<int>(type: "int", nullable: false),
                    DisciplineId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PositionDisciplines", x => new { x.PositionId, x.DisciplineId });
                    table.ForeignKey(
                        name: "FK_PositionDisciplines_Disciplines_DisciplineId",
                        column: x => x.DisciplineId,
                        principalTable: "Disciplines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PositionDisciplines_Positions_PositionId",
                        column: x => x.PositionId,
                        principalTable: "Positions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PositionDisciplines_DisciplineId",
                table: "PositionDisciplines",
                column: "DisciplineId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PositionDisciplines");
        }
    }
}
