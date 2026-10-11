using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeInspector.Leads.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "leads");

            migrationBuilder.CreateTable(
                name: "companies",
                schema: "leads",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    NormalizedName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Website = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    NormalizedWebsite = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Industry = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    EmployeeRange = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Country = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    TechnologySignals = table.Column<string>(type: "jsonb", nullable: false),
                    Evidence = table.Column<string>(type: "jsonb", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_companies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "import_batches",
                schema: "leads",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TotalRows = table.Column<int>(type: "integer", nullable: false),
                    ImportedRows = table.Column<int>(type: "integer", nullable: false),
                    DuplicateRows = table.Column<int>(type: "integer", nullable: false),
                    RejectedRows = table.Column<int>(type: "integer", nullable: false),
                    RowErrors = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_import_batches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "research_sessions",
                schema: "leads",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Keywords = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    SearchUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    OpenedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_research_sessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "suppressed_contacts",
                schema: "leads",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    NormalizedProfileUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_suppressed_contacts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "processing_jobs",
                schema: "leads",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    RunKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Attempts = table.Column<int>(type: "integer", nullable: false),
                    NextAttemptAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastError = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_processing_jobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_processing_jobs_companies_CompanyId",
                        column: x => x.CompanyId,
                        principalSchema: "leads",
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "leads",
                schema: "leads",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    ResearchSessionId = table.Column<Guid>(type: "uuid", nullable: true),
                    FullName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    NormalizedName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    JobTitle = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    LinkedInProfileUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    NormalizedProfileUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    BusinessEmail = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    FitScore = table.Column<int>(type: "integer", nullable: false),
                    ScoreReasons = table.Column<string>(type: "jsonb", nullable: false),
                    LastVerifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_leads", x => x.Id);
                    table.ForeignKey(
                        name: "FK_leads_companies_CompanyId",
                        column: x => x.CompanyId,
                        principalSchema: "leads",
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_leads_research_sessions_ResearchSessionId",
                        column: x => x.ResearchSessionId,
                        principalSchema: "leads",
                        principalTable: "research_sessions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "lead_sources",
                schema: "leads",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    LeadId = table.Column<Guid>(type: "uuid", nullable: true),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: true),
                    SourceType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SourceUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CollectionMethod = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CollectedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PermittedUseNotes = table.Column<string>(type: "text", nullable: true),
                    Evidence = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lead_sources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_lead_sources_companies_CompanyId",
                        column: x => x.CompanyId,
                        principalSchema: "leads",
                        principalTable: "companies",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_lead_sources_leads_LeadId",
                        column: x => x.LeadId,
                        principalSchema: "leads",
                        principalTable: "leads",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "message_drafts",
                schema: "leads",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    LeadId = table.Column<Guid>(type: "uuid", nullable: false),
                    Channel = table.Column<int>(type: "integer", nullable: false),
                    TemplateVersion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Subject = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Body = table.Column<string>(type: "text", nullable: false),
                    EvidenceUsed = table.Column<string>(type: "jsonb", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_message_drafts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_message_drafts_leads_LeadId",
                        column: x => x.LeadId,
                        principalSchema: "leads",
                        principalTable: "leads",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "outreach_activities",
                schema: "leads",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    LeadId = table.Column<Guid>(type: "uuid", nullable: false),
                    DraftId = table.Column<Guid>(type: "uuid", nullable: true),
                    ActivityType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Notes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outreach_activities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_outreach_activities_leads_LeadId",
                        column: x => x.LeadId,
                        principalSchema: "leads",
                        principalTable: "leads",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_outreach_activities_message_drafts_DraftId",
                        column: x => x.DraftId,
                        principalSchema: "leads",
                        principalTable: "message_drafts",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_companies_UserId_NormalizedName_Country",
                schema: "leads",
                table: "companies",
                columns: new[] { "UserId", "NormalizedName", "Country" });

            migrationBuilder.CreateIndex(
                name: "IX_companies_UserId_NormalizedWebsite",
                schema: "leads",
                table: "companies",
                columns: new[] { "UserId", "NormalizedWebsite" },
                unique: true,
                filter: "\"NormalizedWebsite\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_lead_sources_CompanyId",
                schema: "leads",
                table: "lead_sources",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_lead_sources_LeadId",
                schema: "leads",
                table: "lead_sources",
                column: "LeadId");

            migrationBuilder.CreateIndex(
                name: "IX_leads_CompanyId",
                schema: "leads",
                table: "leads",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_leads_ResearchSessionId",
                schema: "leads",
                table: "leads",
                column: "ResearchSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_leads_UserId_BusinessEmail",
                schema: "leads",
                table: "leads",
                columns: new[] { "UserId", "BusinessEmail" },
                unique: true,
                filter: "\"BusinessEmail\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_leads_UserId_NormalizedProfileUrl",
                schema: "leads",
                table: "leads",
                columns: new[] { "UserId", "NormalizedProfileUrl" },
                unique: true,
                filter: "\"NormalizedProfileUrl\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_message_drafts_LeadId",
                schema: "leads",
                table: "message_drafts",
                column: "LeadId");

            migrationBuilder.CreateIndex(
                name: "IX_message_drafts_UserId_LeadId_TemplateVersion_Status",
                schema: "leads",
                table: "message_drafts",
                columns: new[] { "UserId", "LeadId", "TemplateVersion", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_outreach_activities_DraftId",
                schema: "leads",
                table: "outreach_activities",
                column: "DraftId");

            migrationBuilder.CreateIndex(
                name: "IX_outreach_activities_LeadId",
                schema: "leads",
                table: "outreach_activities",
                column: "LeadId");

            migrationBuilder.CreateIndex(
                name: "IX_outreach_activities_UserId_LeadId_OccurredAt",
                schema: "leads",
                table: "outreach_activities",
                columns: new[] { "UserId", "LeadId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_processing_jobs_CompanyId",
                schema: "leads",
                table: "processing_jobs",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_processing_jobs_Status_NextAttemptAt",
                schema: "leads",
                table: "processing_jobs",
                columns: new[] { "Status", "NextAttemptAt" });

            migrationBuilder.CreateIndex(
                name: "IX_processing_jobs_UserId_CompanyId_RunKey",
                schema: "leads",
                table: "processing_jobs",
                columns: new[] { "UserId", "CompanyId", "RunKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_research_sessions_UserId_OpenedAt",
                schema: "leads",
                table: "research_sessions",
                columns: new[] { "UserId", "OpenedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_suppressed_contacts_UserId_NormalizedEmail",
                schema: "leads",
                table: "suppressed_contacts",
                columns: new[] { "UserId", "NormalizedEmail" },
                unique: true,
                filter: "\"NormalizedEmail\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_suppressed_contacts_UserId_NormalizedProfileUrl",
                schema: "leads",
                table: "suppressed_contacts",
                columns: new[] { "UserId", "NormalizedProfileUrl" },
                unique: true,
                filter: "\"NormalizedProfileUrl\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "import_batches",
                schema: "leads");

            migrationBuilder.DropTable(
                name: "lead_sources",
                schema: "leads");

            migrationBuilder.DropTable(
                name: "outreach_activities",
                schema: "leads");

            migrationBuilder.DropTable(
                name: "processing_jobs",
                schema: "leads");

            migrationBuilder.DropTable(
                name: "suppressed_contacts",
                schema: "leads");

            migrationBuilder.DropTable(
                name: "message_drafts",
                schema: "leads");

            migrationBuilder.DropTable(
                name: "leads",
                schema: "leads");

            migrationBuilder.DropTable(
                name: "companies",
                schema: "leads");

            migrationBuilder.DropTable(
                name: "research_sessions",
                schema: "leads");
        }
    }
}
