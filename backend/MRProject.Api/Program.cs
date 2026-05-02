using MRProject.Api.Common;
using MRProject.Api.Data;
using MRProject.Api.Entities;
using MRProject.Api.Enums;
using MRProject.Api.Middleware;
using MRProject.Api.Services;
using MRProject.Api.Services.DocumentParsing;
using MRProject.Api.Services.Interfaces;
using MRProject.Api.Services.Llm;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Microsoft.Extensions.Options;
using System.Data;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
builder.Services.Configure<AdminSeedOptions>(builder.Configuration.GetSection("AdminSeed"));
builder.Services.Configure<FileStorageOptions>(builder.Configuration.GetSection("FileStorage"));
builder.Services.Configure<LlmOptions>(builder.Configuration.GetSection("Llm"));
builder.Services.Configure<OperationRecordCleanupOptions>(builder.Configuration.GetSection("OperationRecordCleanup"));

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is missing.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient<OpenAiCompatibleLlmService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IDocumentService, DocumentService>();
builder.Services.AddScoped<IDocumentParserService, DocumentParserService>();
builder.Services.AddScoped<ILlmService>(serviceProvider =>
{
    var options = serviceProvider.GetRequiredService<IOptions<LlmOptions>>().Value;
    return options.UseMock
        ? serviceProvider.GetRequiredService<MockLlmService>()
        : serviceProvider.GetRequiredService<OpenAiCompatibleLlmService>();
});
builder.Services.AddScoped<MockLlmService>();
builder.Services.AddScoped<IScgService, ScgService>();
builder.Services.AddScoped<IMrService, MrService>();
builder.Services.AddScoped<IExportService, ExportService>();
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddHostedService<OperationRecordCleanupService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "MR Project API",
        Version = "v1"
    });

    var jwtSecurityScheme = new OpenApiSecurityScheme
    {
        Scheme = "bearer",
        BearerFormat = "JWT",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Description = "Input your JWT token in this format: Bearer {token}",
        Reference = new OpenApiReference
        {
            Id = JwtBearerDefaults.AuthenticationScheme,
            Type = ReferenceType.SecurityScheme
        }
    };

    options.AddSecurityDefinition(jwtSecurityScheme.Reference.Id, jwtSecurityScheme);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { jwtSecurityScheme, Array.Empty<string>() }
    });
});

var jwtOptions = builder.Configuration.GetSection("Jwt").Get<JwtOptions>()
    ?? throw new InvalidOperationException("Jwt settings are missing.");

var jsonOptions = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
};

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey)),
            ClockSkew = TimeSpan.Zero
        };
        options.Events = new JwtBearerEvents
        {
            OnChallenge = async context =>
            {
                context.HandleResponse();
                context.Response.StatusCode = StatusCodes.Status200OK;
                context.Response.ContentType = "application/json";
                var response = ApiResponse<object>.Fail(401, "未登录或 token 无效");
                await context.Response.WriteAsync(JsonSerializer.Serialize(response, jsonOptions));
            },
            OnForbidden = async context =>
            {
                context.Response.StatusCode = StatusCodes.Status200OK;
                context.Response.ContentType = "application/json";
                var response = ApiResponse<object>.Fail(403, "无权限访问");
                await context.Response.WriteAsync(JsonSerializer.Serialize(response, jsonOptions));
            }
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseMiddleware<ExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var dbContext = services.GetRequiredService<ApplicationDbContext>();
    await dbContext.Database.EnsureCreatedAsync();
    await EnsureUsersTableAsync(dbContext);
    await EnsureDocumentsTableAsync(dbContext);
    await EnsureScgTablesAsync(dbContext);
    await EnsureMrTablesAsync(dbContext);
    await EnsureScgHistoryTablesAsync(dbContext);
    await EnsureUserOperationTablesAsync(dbContext);
    await EnsureOperationRecordCleanupSettingTablesAsync(dbContext);

    var hasher = services.GetRequiredService<IPasswordHasher<User>>();
    var configuration = services.GetRequiredService<IConfiguration>();
    await SeedAdminAsync(dbContext, hasher, configuration);
}

app.Run();

static async Task EnsureUsersTableAsync(ApplicationDbContext dbContext)
{
    await EnsureColumnExistsAsync(dbContext, "users", "profile_description", "ALTER TABLE users ADD COLUMN profile_description varchar(500) CHARACTER SET utf8mb4 NOT NULL DEFAULT '' AFTER phone;");
}
static async Task EnsureDocumentsTableAsync(ApplicationDbContext dbContext)
{
    const string sql = @"
CREATE TABLE IF NOT EXISTS `documents` (
    `id` bigint NOT NULL AUTO_INCREMENT,
    `user_id` bigint NOT NULL,
    `document_name` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `original_file_name` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `stored_file_name` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `file_path` varchar(500) CHARACTER SET utf8mb4 NOT NULL,
    `file_type` varchar(20) CHARACTER SET utf8mb4 NOT NULL,
    `file_size` bigint NOT NULL,
    `process_status` varchar(30) CHARACTER SET utf8mb4 NOT NULL,
    `is_deleted` tinyint(1) NOT NULL,
    `created_at` datetime(6) NOT NULL,
    `updated_at` datetime(6) NOT NULL,
    CONSTRAINT `PK_documents` PRIMARY KEY (`id`),
    CONSTRAINT `FK_documents_users_user_id` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;";

    await dbContext.Database.ExecuteSqlRawAsync(sql);
}

static async Task EnsureScgTablesAsync(ApplicationDbContext dbContext)
{
    const string scgSql = @"
CREATE TABLE IF NOT EXISTS `scg_records` (
    `id` bigint NOT NULL AUTO_INCREMENT,
    `document_id` bigint NOT NULL,
    `user_id` bigint NOT NULL,
    `document_ids_key` varchar(500) CHARACTER SET utf8mb4 NOT NULL DEFAULT '',
    `document_names_summary` varchar(1000) CHARACTER SET utf8mb4 NOT NULL DEFAULT '',
    `scg_json` longtext NOT NULL,
    `source_text_snapshot` longtext NOT NULL,
    `is_confirmed` tinyint(1) NOT NULL DEFAULT 0,
    `confirmed_at` datetime(6) NULL,
    `is_deleted` tinyint(1) NOT NULL,
    `created_at` datetime(6) NOT NULL,
    `updated_at` datetime(6) NOT NULL,
    CONSTRAINT `PK_scg_records` PRIMARY KEY (`id`),
    CONSTRAINT `FK_scg_records_documents_document_id` FOREIGN KEY (`document_id`) REFERENCES `documents` (`id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_scg_records_users_user_id` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;";

    const string llmLogSql = @"
CREATE TABLE IF NOT EXISTS `llm_call_logs` (
    `id` bigint NOT NULL AUTO_INCREMENT,
    `business_type` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
    `business_id` bigint NOT NULL,
    `prompt_content` longtext NOT NULL,
    `response_content` longtext NOT NULL,
    `call_status` varchar(30) CHARACTER SET utf8mb4 NOT NULL,
    `error_message` longtext NOT NULL,
    `created_at` datetime(6) NOT NULL,
    `updated_at` datetime(6) NOT NULL,
    CONSTRAINT `PK_llm_call_logs` PRIMARY KEY (`id`)
) CHARACTER SET=utf8mb4;";

    await dbContext.Database.ExecuteSqlRawAsync(scgSql);
    await EnsureColumnExistsAsync(dbContext, "scg_records", "document_ids_key", "ALTER TABLE `scg_records` ADD COLUMN `document_ids_key` varchar(500) CHARACTER SET utf8mb4 NOT NULL DEFAULT '' AFTER `user_id`;");
    await EnsureColumnExistsAsync(dbContext, "scg_records", "document_names_summary", "ALTER TABLE `scg_records` ADD COLUMN `document_names_summary` varchar(1000) CHARACTER SET utf8mb4 NOT NULL DEFAULT '' AFTER `document_ids_key`;");
    await EnsureColumnExistsAsync(dbContext, "scg_records", "is_confirmed", "ALTER TABLE `scg_records` ADD COLUMN `is_confirmed` tinyint(1) NOT NULL DEFAULT 0 AFTER `source_text_snapshot`;");
    await EnsureColumnExistsAsync(dbContext, "scg_records", "confirmed_at", "ALTER TABLE `scg_records` ADD COLUMN `confirmed_at` datetime(6) NULL AFTER `is_confirmed`;");
    await dbContext.Database.ExecuteSqlRawAsync(llmLogSql);
    await EnsureLongTextColumnAsync(dbContext, "llm_call_logs", "error_message");
    await EnsureLongTextColumnAsync(dbContext, "llm_call_logs", "prompt_content");
    await EnsureLongTextColumnAsync(dbContext, "llm_call_logs", "response_content");
}


static async Task EnsureOperationRecordCleanupSettingTablesAsync(ApplicationDbContext dbContext)
{
    const string cleanupSettingSql = @"
CREATE TABLE IF NOT EXISTS operation_record_cleanup_settings (
    id bigint NOT NULL AUTO_INCREMENT,
    enabled tinyint(1) NOT NULL,
    retention_days int NOT NULL,
    interval_value int NOT NULL,
    interval_unit varchar(20) CHARACTER SET utf8mb4 NOT NULL,
    last_cleanup_at datetime(6) NULL,
    created_at datetime(6) NOT NULL,
    updated_at datetime(6) NOT NULL,
    CONSTRAINT PK_operation_record_cleanup_settings PRIMARY KEY (id)
) CHARACTER SET=utf8mb4;";

    const string userCleanupSettingSql = @"
CREATE TABLE IF NOT EXISTS user_operation_cleanup_settings (
    id bigint NOT NULL AUTO_INCREMENT,
    user_id bigint NOT NULL,
    enabled tinyint(1) NOT NULL,
    retention_days int NOT NULL,
    interval_value int NOT NULL,
    interval_unit varchar(20) CHARACTER SET utf8mb4 NOT NULL,
    last_cleanup_at datetime(6) NULL,
    created_at datetime(6) NOT NULL,
    updated_at datetime(6) NOT NULL,
    CONSTRAINT PK_user_operation_cleanup_settings PRIMARY KEY (id),
    CONSTRAINT FK_user_operation_cleanup_settings_users_user_id FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;";

    await dbContext.Database.ExecuteSqlRawAsync(cleanupSettingSql);
    await dbContext.Database.ExecuteSqlRawAsync(userCleanupSettingSql);

    await EnsureColumnExistsAsync(dbContext, "operation_record_cleanup_settings", "interval_value", "ALTER TABLE `operation_record_cleanup_settings` ADD COLUMN `interval_value` int NOT NULL DEFAULT 24 AFTER `retention_days`;");
    await EnsureColumnExistsAsync(dbContext, "operation_record_cleanup_settings", "interval_unit", "ALTER TABLE `operation_record_cleanup_settings` ADD COLUMN `interval_unit` varchar(20) CHARACTER SET utf8mb4 NOT NULL DEFAULT 'hour' AFTER `interval_value`;");
    await EnsureColumnExistsAsync(dbContext, "operation_record_cleanup_settings", "last_cleanup_at", "ALTER TABLE `operation_record_cleanup_settings` ADD COLUMN `last_cleanup_at` datetime(6) NULL AFTER `interval_unit`;");
    await dbContext.Database.ExecuteSqlRawAsync("UPDATE `operation_record_cleanup_settings` SET `interval_value` = COALESCE(`interval_hours`, 24) WHERE `interval_value` = 24;");
    await dbContext.Database.ExecuteSqlRawAsync("UPDATE `operation_record_cleanup_settings` SET `interval_unit` = 'hour' WHERE `interval_unit` = '' OR `interval_unit` IS NULL;");

    await EnsureColumnExistsAsync(dbContext, "user_operation_cleanup_settings", "interval_value", "ALTER TABLE `user_operation_cleanup_settings` ADD COLUMN `interval_value` int NOT NULL DEFAULT 24 AFTER `retention_days`;");
    await EnsureColumnExistsAsync(dbContext, "user_operation_cleanup_settings", "interval_unit", "ALTER TABLE `user_operation_cleanup_settings` ADD COLUMN `interval_unit` varchar(20) CHARACTER SET utf8mb4 NOT NULL DEFAULT 'hour' AFTER `interval_value`;");
    await EnsureColumnExistsAsync(dbContext, "user_operation_cleanup_settings", "last_cleanup_at", "ALTER TABLE `user_operation_cleanup_settings` ADD COLUMN `last_cleanup_at` datetime(6) NULL AFTER `interval_unit`;");
}
static async Task EnsureUserOperationTablesAsync(ApplicationDbContext dbContext)
{
    const string userOperationSql = @"
CREATE TABLE IF NOT EXISTS user_operation_records (
    id bigint NOT NULL AUTO_INCREMENT,
    user_id bigint NOT NULL,
    operation_type varchar(30) CHARACTER SET utf8mb4 NOT NULL,
    operation_target varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    operation_snapshot longtext NOT NULL,
    created_at datetime(6) NOT NULL,
    updated_at datetime(6) NOT NULL,
    CONSTRAINT PK_user_operation_records PRIMARY KEY (id),
    CONSTRAINT FK_user_operation_records_users_user_id FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;";

    await dbContext.Database.ExecuteSqlRawAsync(userOperationSql);
}
static async Task EnsureScgHistoryTablesAsync(ApplicationDbContext dbContext)
{
    const string scgHistorySql = @"
CREATE TABLE IF NOT EXISTS scg_history_records (
    id bigint NOT NULL AUTO_INCREMENT,
    scg_record_id bigint NOT NULL,
    user_id bigint NOT NULL,
    scg_name varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    operation_type varchar(30) CHARACTER SET utf8mb4 NOT NULL,
    scg_json longtext NOT NULL,
    created_at datetime(6) NOT NULL,
    updated_at datetime(6) NOT NULL,
    CONSTRAINT PK_scg_history_records PRIMARY KEY (id),
    CONSTRAINT FK_scg_history_records_scg_records_scg_record_id FOREIGN KEY (scg_record_id) REFERENCES scg_records (id) ON DELETE RESTRICT,
    CONSTRAINT FK_scg_history_records_users_user_id FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;";

    await dbContext.Database.ExecuteSqlRawAsync(scgHistorySql);
}
static async Task EnsureMrTablesAsync(ApplicationDbContext dbContext)
{
    const string mrSql = @"
CREATE TABLE IF NOT EXISTS mr_records (
    id bigint NOT NULL AUTO_INCREMENT,
    scg_record_id bigint NOT NULL,
    user_id bigint NOT NULL,
    document_ids_key varchar(500) CHARACTER SET utf8mb4 NOT NULL,
    document_names_summary varchar(1000) CHARACTER SET utf8mb4 NOT NULL,
    mr_json longtext NOT NULL,
    is_deleted tinyint(1) NOT NULL,
    created_at datetime(6) NOT NULL,
    updated_at datetime(6) NOT NULL,
    CONSTRAINT PK_mr_records PRIMARY KEY (id),
    CONSTRAINT FK_mr_records_scg_records_scg_record_id FOREIGN KEY (scg_record_id) REFERENCES scg_records (id) ON DELETE RESTRICT,
    CONSTRAINT FK_mr_records_users_user_id FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;";

    const string mrHistorySql = @"
CREATE TABLE IF NOT EXISTS mr_history_records (
    id bigint NOT NULL AUTO_INCREMENT,
    mr_record_id bigint NOT NULL,
    user_id bigint NOT NULL,
    operation_type varchar(30) CHARACTER SET utf8mb4 NOT NULL,
    mr_json longtext NOT NULL,
    created_at datetime(6) NOT NULL,
    updated_at datetime(6) NOT NULL,
    CONSTRAINT PK_mr_history_records PRIMARY KEY (id),
    CONSTRAINT FK_mr_history_records_mr_records_mr_record_id FOREIGN KEY (mr_record_id) REFERENCES mr_records (id) ON DELETE RESTRICT,
    CONSTRAINT FK_mr_history_records_users_user_id FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;";

    await dbContext.Database.ExecuteSqlRawAsync(mrSql);
    await dbContext.Database.ExecuteSqlRawAsync(mrHistorySql);
}
static async Task<bool> ColumnExistsAsync(ApplicationDbContext dbContext, string tableName, string columnName)
{
    var connection = dbContext.Database.GetDbConnection();
    var shouldClose = connection.State != ConnectionState.Open;
    if (shouldClose)
    {
        await connection.OpenAsync();
    }

    try
    {
        await using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT COUNT(*)
FROM information_schema.columns
WHERE table_schema = DATABASE()
  AND table_name = @tableName
  AND column_name = @columnName";

        var tableParam = command.CreateParameter();
        tableParam.ParameterName = "@tableName";
        tableParam.Value = tableName;
        command.Parameters.Add(tableParam);

        var columnParam = command.CreateParameter();
        columnParam.ParameterName = "@columnName";
        columnParam.Value = columnName;
        command.Parameters.Add(columnParam);

        var result = await command.ExecuteScalarAsync();
        var count = result is null || result == DBNull.Value ? 0 : Convert.ToInt32(result);
        return count > 0;
    }
    finally
    {
        if (shouldClose)
        {
            await connection.CloseAsync();
        }
    }
}
static async Task EnsureColumnExistsAsync(ApplicationDbContext dbContext, string tableName, string columnName, string alterSql)
{
    var connection = dbContext.Database.GetDbConnection();
    var shouldClose = connection.State != ConnectionState.Open;
    if (shouldClose)
    {
        await connection.OpenAsync();
    }

    try
    {
        await using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT COUNT(*)
FROM information_schema.columns
WHERE table_schema = DATABASE()
  AND table_name = @tableName
  AND column_name = @columnName";

        var tableParam = command.CreateParameter();
        tableParam.ParameterName = "@tableName";
        tableParam.Value = tableName;
        command.Parameters.Add(tableParam);

        var columnParam = command.CreateParameter();
        columnParam.ParameterName = "@columnName";
        columnParam.Value = columnName;
        command.Parameters.Add(columnParam);

        var result = await command.ExecuteScalarAsync();
        var count = result is null || result == DBNull.Value ? 0 : Convert.ToInt32(result);

        if (count == 0)
        {
            await dbContext.Database.ExecuteSqlRawAsync(alterSql);
        }
    }
    finally
    {
        if (shouldClose)
        {
            await connection.CloseAsync();
        }
    }
}

static async Task EnsureLongTextColumnAsync(ApplicationDbContext dbContext, string tableName, string columnName)
{
    var connection = dbContext.Database.GetDbConnection();
    var shouldClose = connection.State != ConnectionState.Open;
    if (shouldClose)
    {
        await connection.OpenAsync();
    }

    try
    {
        await using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT DATA_TYPE
FROM information_schema.columns
WHERE table_schema = DATABASE()
  AND table_name = @tableName
  AND column_name = @columnName";

        var tableParam = command.CreateParameter();
        tableParam.ParameterName = "@tableName";
        tableParam.Value = tableName;
        command.Parameters.Add(tableParam);

        var columnParam = command.CreateParameter();
        columnParam.ParameterName = "@columnName";
        columnParam.Value = columnName;
        command.Parameters.Add(columnParam);

        var result = await command.ExecuteScalarAsync();
        var dataType = result?.ToString()?.ToLowerInvariant() ?? string.Empty;
        if (dataType != "longtext")
        {
            await dbContext.Database.ExecuteSqlRawAsync($"ALTER TABLE `{tableName}` MODIFY COLUMN `{columnName}` longtext NOT NULL;");
        }
    }
    finally
    {
        if (shouldClose)
        {
            await connection.CloseAsync();
        }
    }
}

static async Task SeedAdminAsync(
    ApplicationDbContext dbContext,
    IPasswordHasher<User> passwordHasher,
    IConfiguration configuration)
{
    var adminUsername = configuration["AdminSeed:Username"];
    var adminPassword = configuration["AdminSeed:Password"];
    var adminRealName = configuration["AdminSeed:RealName"] ?? "System Admin";

    if (string.IsNullOrWhiteSpace(adminUsername) || string.IsNullOrWhiteSpace(adminPassword))
    {
        return;
    }

    var exists = await dbContext.Users.AnyAsync(user => user.Username == adminUsername && !user.IsDeleted);
    if (exists)
    {
        return;
    }

    var now = SystemTime.Now();

    var admin = new User
    {
        Username = adminUsername,
        RealName = adminRealName,
        Role = UserRole.Admin,
        Status = UserStatus.Enabled,
        Email = "admin@example.com",
        CreatedAt = now,
        UpdatedAt = now,
        IsDeleted = false
    };

    admin.PasswordHash = passwordHasher.HashPassword(admin, adminPassword);
    dbContext.Users.Add(admin);
    await dbContext.SaveChangesAsync();
}




















