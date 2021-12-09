using System;
using System.IO;
using Microsoft.EntityFrameworkCore.Migrations;

namespace WJS_MovieLens.Migrations
{
    public partial class InsertOccupations : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var sqlFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "SQL", @"3-InsertOccupations.sql");
            migrationBuilder.Sql(File.ReadAllText(sqlFile));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("delete from occupations");
        }
    }
}
