using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AI.Data.Migrations
{
    public partial class ChangeEmbeddingToVarbinary : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Alter the embedding column to varbinary(max). Use raw SQL to ensure provider applies correct type.
            migrationBuilder.Sql(@"IF EXISTS(SELECT 1 FROM sys.columns c JOIN sys.tables t ON c.object_id = t.object_id WHERE t.name = 'documentchunk' AND c.name = 'embedding')
BEGIN
    ALTER TABLE [dbo].[documentchunk] ALTER COLUMN [embedding] varbinary(max) NULL;
END");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Attempt to revert the column type back to vector. If the provider does not accept 'vector' via AlterColumn,
            // use raw SQL to alter the column type back. Adjust schema/ownership if needed.
            migrationBuilder.Sql(@"IF EXISTS(SELECT 1 FROM sys.columns c JOIN sys.tables t ON c.object_id = t.object_id WHERE t.name = 'documentchunk' AND c.name = 'embedding')
BEGIN
    ALTER TABLE [dbo].[documentchunk] ALTER COLUMN [embedding] vector NULL;
END");
        }
    }
}
