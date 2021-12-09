using System;
using System.IO;
using Microsoft.EntityFrameworkCore.Migrations;

namespace WJS_MovieLens.Migrations
{
    public partial class InsertGenres : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var sqlFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "SQL", @"1-InsertGenres.sql");
            migrationBuilder.Sql(File.ReadAllText(sqlFile));

        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("delete from genres");
        }
    }
}
