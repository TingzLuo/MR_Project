using MRProject.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace MRProject.Api.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<ScgRecord> ScgRecords => Set<ScgRecord>();
    public DbSet<ScgHistoryRecord> ScgHistoryRecords => Set<ScgHistoryRecord>();
    public DbSet<UserOperationRecord> UserOperationRecords => Set<UserOperationRecord>();
    public DbSet<OperationRecordCleanupSetting> OperationRecordCleanupSettings => Set<OperationRecordCleanupSetting>();
    public DbSet<UserOperationCleanupSetting> UserOperationCleanupSettings => Set<UserOperationCleanupSetting>();
    public DbSet<LlmCallLog> LlmCallLogs => Set<LlmCallLog>();
    public DbSet<MrRecord> MrRecords => Set<MrRecord>();
    public DbSet<MrHistoryRecord> MrHistoryRecords => Set<MrHistoryRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(item => item.Id);
            entity.HasIndex(item => item.Username).IsUnique();
            entity.Property(item => item.Id).HasColumnName("id");
            entity.Property(item => item.Username).HasColumnName("username").HasMaxLength(50).IsRequired();
            entity.Property(item => item.PasswordHash).HasColumnName("password_hash").HasMaxLength(255).IsRequired();
            entity.Property(item => item.RealName).HasColumnName("real_name").HasMaxLength(50);
            entity.Property(item => item.Email).HasColumnName("email").HasMaxLength(100);
            entity.Property(item => item.Phone).HasColumnName("phone").HasMaxLength(20);
            entity.Property(item => item.ProfileDescription).HasColumnName("profile_description").HasMaxLength(500);
            entity.Property(item => item.Role).HasColumnName("role").HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.Property(item => item.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.Property(item => item.LastLoginAt).HasColumnName("last_login_at");
            entity.Property(item => item.CreatedAt).HasColumnName("created_at").IsRequired();
            entity.Property(item => item.UpdatedAt).HasColumnName("updated_at").IsRequired();
            entity.Property(item => item.IsDeleted).HasColumnName("is_deleted").IsRequired();
        });

        modelBuilder.Entity<Document>(entity =>
        {
            entity.ToTable("documents");
            entity.HasKey(item => item.Id);
            entity.HasIndex(item => new { item.UserId, item.CreatedAt });
            entity.Property(item => item.Id).HasColumnName("id");
            entity.Property(item => item.UserId).HasColumnName("user_id").IsRequired();
            entity.Property(item => item.DocumentName).HasColumnName("document_name").HasMaxLength(255).IsRequired();
            entity.Property(item => item.OriginalFileName).HasColumnName("original_file_name").HasMaxLength(255).IsRequired();
            entity.Property(item => item.StoredFileName).HasColumnName("stored_file_name").HasMaxLength(255).IsRequired();
            entity.Property(item => item.FilePath).HasColumnName("file_path").HasMaxLength(500).IsRequired();
            entity.Property(item => item.FileType).HasColumnName("file_type").HasMaxLength(20).IsRequired();
            entity.Property(item => item.FileSize).HasColumnName("file_size").IsRequired();
            entity.Property(item => item.ProcessStatus).HasColumnName("process_status").HasConversion<string>().HasMaxLength(30).IsRequired();
            entity.Property(item => item.CreatedAt).HasColumnName("created_at").IsRequired();
            entity.Property(item => item.UpdatedAt).HasColumnName("updated_at").IsRequired();
            entity.Property(item => item.IsDeleted).HasColumnName("is_deleted").IsRequired();
        });

        modelBuilder.Entity<ScgRecord>(entity =>
        {
            entity.ToTable("scg_records");
            entity.HasKey(item => item.Id);
            entity.HasIndex(item => new { item.DocumentId, item.UserId });
            entity.HasIndex(item => new { item.UserId, item.DocumentIdsKey });
            entity.Property(item => item.Id).HasColumnName("id");
            entity.Property(item => item.DocumentId).HasColumnName("document_id").IsRequired();
            entity.Property(item => item.UserId).HasColumnName("user_id").IsRequired();
            entity.Property(item => item.ScgName).HasColumnName("scg_name").HasMaxLength(255).IsRequired();
            entity.Property(item => item.DocumentIdsKey).HasColumnName("document_ids_key").HasMaxLength(500).IsRequired();
            entity.Property(item => item.DocumentNamesSummary).HasColumnName("document_names_summary").HasMaxLength(1000).IsRequired();
            entity.Property(item => item.ScgJson).HasColumnName("scg_json").HasColumnType("longtext").IsRequired();
            entity.Property(item => item.SourceTextSnapshot).HasColumnName("source_text_snapshot").HasColumnType("longtext").IsRequired();
            entity.Property(item => item.IsConfirmed).HasColumnName("is_confirmed").IsRequired();
            entity.Property(item => item.ConfirmedAt).HasColumnName("confirmed_at");
            entity.Property(item => item.CreatedAt).HasColumnName("created_at").IsRequired();
            entity.Property(item => item.UpdatedAt).HasColumnName("updated_at").IsRequired();
            entity.Property(item => item.IsDeleted).HasColumnName("is_deleted").IsRequired();
        });

        modelBuilder.Entity<ScgHistoryRecord>(entity =>
        {
            entity.ToTable("scg_history_records");
            entity.HasKey(item => item.Id);
            entity.HasIndex(item => new { item.ScgRecordId, item.UserId });
            entity.Property(item => item.Id).HasColumnName("id");
            entity.Property(item => item.ScgRecordId).HasColumnName("scg_record_id").IsRequired();
            entity.Property(item => item.UserId).HasColumnName("user_id").IsRequired();
            entity.Property(item => item.ScgName).HasColumnName("scg_name").HasMaxLength(255).IsRequired();
            entity.Property(item => item.OperationType).HasColumnName("operation_type").HasMaxLength(30).IsRequired();
            entity.Property(item => item.ScgJson).HasColumnName("scg_json").HasColumnType("longtext").IsRequired();
            entity.Property(item => item.CreatedAt).HasColumnName("created_at").IsRequired();
            entity.Property(item => item.UpdatedAt).HasColumnName("updated_at").IsRequired();
        });

        modelBuilder.Entity<UserOperationRecord>(entity =>
        {
            entity.ToTable("user_operation_records");
            entity.HasKey(item => item.Id);
            entity.HasIndex(item => new { item.UserId, item.CreatedAt });
            entity.Property(item => item.Id).HasColumnName("id");
            entity.Property(item => item.UserId).HasColumnName("user_id").IsRequired();
            entity.Property(item => item.OperationType).HasColumnName("operation_type").HasMaxLength(30).IsRequired();
            entity.Property(item => item.OperationTarget).HasColumnName("operation_target").HasMaxLength(255).IsRequired();
            entity.Property(item => item.OperationSnapshot).HasColumnName("operation_snapshot").HasColumnType("longtext").IsRequired();
            entity.Property(item => item.CreatedAt).HasColumnName("created_at").IsRequired();
            entity.Property(item => item.UpdatedAt).HasColumnName("updated_at").IsRequired();
        });

        modelBuilder.Entity<OperationRecordCleanupSetting>(entity =>
        {
            entity.ToTable("operation_record_cleanup_settings");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Id).HasColumnName("id");
            entity.Property(item => item.Enabled).HasColumnName("enabled").IsRequired();
            entity.Property(item => item.RetentionDays).HasColumnName("retention_days").IsRequired();
            entity.Property(item => item.IntervalValue).HasColumnName("interval_value").IsRequired();
            entity.Property(item => item.IntervalUnit).HasColumnName("interval_unit").HasMaxLength(20).IsRequired();
            entity.Property(item => item.LastCleanupAt).HasColumnName("last_cleanup_at");
            entity.Property(item => item.CreatedAt).HasColumnName("created_at").IsRequired();
            entity.Property(item => item.UpdatedAt).HasColumnName("updated_at").IsRequired();
        });

        modelBuilder.Entity<UserOperationCleanupSetting>(entity =>
        {
            entity.ToTable("user_operation_cleanup_settings");
            entity.HasKey(item => item.Id);
            entity.HasIndex(item => item.UserId).IsUnique();
            entity.Property(item => item.Id).HasColumnName("id");
            entity.Property(item => item.UserId).HasColumnName("user_id").IsRequired();
            entity.Property(item => item.Enabled).HasColumnName("enabled").IsRequired();
            entity.Property(item => item.RetentionDays).HasColumnName("retention_days").IsRequired();
            entity.Property(item => item.IntervalValue).HasColumnName("interval_value").IsRequired();
            entity.Property(item => item.IntervalUnit).HasColumnName("interval_unit").HasMaxLength(20).IsRequired();
            entity.Property(item => item.LastCleanupAt).HasColumnName("last_cleanup_at");
            entity.Property(item => item.CreatedAt).HasColumnName("created_at").IsRequired();
            entity.Property(item => item.UpdatedAt).HasColumnName("updated_at").IsRequired();
        });

        modelBuilder.Entity<LlmCallLog>(entity =>
        {
            entity.ToTable("llm_call_logs");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Id).HasColumnName("id");
            entity.Property(item => item.BusinessType).HasColumnName("business_type").HasMaxLength(50).IsRequired();
            entity.Property(item => item.BusinessId).HasColumnName("business_id").IsRequired();
            entity.Property(item => item.PromptContent).HasColumnName("prompt_content").HasColumnType("longtext").IsRequired();
            entity.Property(item => item.ResponseContent).HasColumnName("response_content").HasColumnType("longtext").IsRequired();
            entity.Property(item => item.CallStatus).HasColumnName("call_status").HasMaxLength(30).IsRequired();
            entity.Property(item => item.ErrorMessage).HasColumnName("error_message").HasColumnType("longtext").IsRequired();
            entity.Property(item => item.CreatedAt).HasColumnName("created_at").IsRequired();
            entity.Property(item => item.UpdatedAt).HasColumnName("updated_at").IsRequired();
        });

        modelBuilder.Entity<MrRecord>(entity =>
        {
            entity.ToTable("mr_records");
            entity.HasKey(item => item.Id);
            entity.HasIndex(item => new { item.ScgRecordId, item.UserId });
            entity.Property(item => item.Id).HasColumnName("id");
            entity.Property(item => item.ScgRecordId).HasColumnName("scg_record_id").IsRequired();
            entity.Property(item => item.UserId).HasColumnName("user_id").IsRequired();
            entity.Property(item => item.DocumentIdsKey).HasColumnName("document_ids_key").HasMaxLength(500).IsRequired();
            entity.Property(item => item.DocumentNamesSummary).HasColumnName("document_names_summary").HasMaxLength(1000).IsRequired();
            entity.Property(item => item.MrJson).HasColumnName("mr_json").HasColumnType("longtext").IsRequired();
            entity.Property(item => item.CreatedAt).HasColumnName("created_at").IsRequired();
            entity.Property(item => item.UpdatedAt).HasColumnName("updated_at").IsRequired();
            entity.Property(item => item.IsDeleted).HasColumnName("is_deleted").IsRequired();
        });

        modelBuilder.Entity<MrHistoryRecord>(entity =>
        {
            entity.ToTable("mr_history_records");
            entity.HasKey(item => item.Id);
            entity.HasIndex(item => new { item.MrRecordId, item.UserId });
            entity.Property(item => item.Id).HasColumnName("id");
            entity.Property(item => item.MrRecordId).HasColumnName("mr_record_id").IsRequired();
            entity.Property(item => item.UserId).HasColumnName("user_id").IsRequired();
            entity.Property(item => item.OperationType).HasColumnName("operation_type").HasMaxLength(30).IsRequired();
            entity.Property(item => item.MrJson).HasColumnName("mr_json").HasColumnType("longtext").IsRequired();
            entity.Property(item => item.CreatedAt).HasColumnName("created_at").IsRequired();
            entity.Property(item => item.UpdatedAt).HasColumnName("updated_at").IsRequired();
        });
    }
}
