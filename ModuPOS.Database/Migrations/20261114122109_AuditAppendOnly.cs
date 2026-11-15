using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ModuPOS.Database.Migrations
{
    /// <summary>
    /// Makes AuditEntries append-only (FR-036):
    /// - an INSTEAD OF UPDATE trigger rejects every update, for every login;
    /// - an AFTER DELETE trigger rejects deletes unless the connection set the
    ///   'modupos.audit_retention' session flag (AuditRetentionJob) and every deleted row is
    ///   older than one year;
    /// - database roles for deployments that use separate logins: modupos_app is denied UPDATE
    ///   and DELETE on the table; modupos_retention may DELETE.
    /// </summary>
    public partial class AuditAppendOnly : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE TRIGGER [dbo].[TR_AuditEntries_NoUpdate]
                ON [dbo].[AuditEntries]
                INSTEAD OF UPDATE
                AS
                BEGIN
                    SET NOCOUNT ON;
                    THROW 50001, 'Audit entries are append-only and cannot be updated.', 1;
                END
                """);

            migrationBuilder.Sql("""
                CREATE TRIGGER [dbo].[TR_AuditEntries_RetentionOnlyDelete]
                ON [dbo].[AuditEntries]
                AFTER DELETE
                AS
                BEGIN
                    SET NOCOUNT ON;
                    IF NOT EXISTS (SELECT 1 FROM deleted) RETURN;

                    IF CAST(SESSION_CONTEXT(N'modupos.audit_retention') AS int) IS NULL
                       OR EXISTS (SELECT 1 FROM deleted WHERE [OccurredAt] >= DATEADD(day, -365, SYSUTCDATETIME()))
                    BEGIN
                        THROW 50002, 'Audit entries can only be removed by the retention job after one year.', 1;
                    END
                END
                """);

            migrationBuilder.Sql("""
                IF DATABASE_PRINCIPAL_ID(N'modupos_app') IS NULL CREATE ROLE [modupos_app];
                IF DATABASE_PRINCIPAL_ID(N'modupos_retention') IS NULL CREATE ROLE [modupos_retention];
                DENY UPDATE, DELETE ON [dbo].[AuditEntries] TO [modupos_app];
                GRANT SELECT, DELETE ON [dbo].[AuditEntries] TO [modupos_retention];
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS [dbo].[TR_AuditEntries_RetentionOnlyDelete];");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS [dbo].[TR_AuditEntries_NoUpdate];");
            migrationBuilder.Sql("""
                IF DATABASE_PRINCIPAL_ID(N'modupos_app') IS NOT NULL DROP ROLE [modupos_app];
                IF DATABASE_PRINCIPAL_ID(N'modupos_retention') IS NOT NULL DROP ROLE [modupos_retention];
                """);
        }
    }
}
