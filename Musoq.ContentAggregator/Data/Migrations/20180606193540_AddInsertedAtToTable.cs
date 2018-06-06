using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace Musoq.ContentAggregator.Data.Migrations
{
    public partial class AddInsertedAtToTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "BatchId",
                table: "Tables",
                nullable: false,
                oldClrType: typeof(int));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "InsertedAt",
                table: "Tables",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InsertedAt",
                table: "Tables");

            migrationBuilder.AlterColumn<int>(
                name: "BatchId",
                table: "Tables",
                nullable: false,
                oldClrType: typeof(Guid));
        }
    }
}
