using System.Text;
using System.Text.Json.Serialization;
using Konbini.Api.Features.Common.Abstractions;
using Konbini.Api.Features.Common.Auth;
using Konbini.Api.Features.Common.Configuration;
using Konbini.Api.Features.Common.Persistence;
using Konbini.Api.Features.Common.Persistence.Seed;
using Konbini.Api.Features.Common.Web;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
// 本機 dotnet run 讀根目錄的 .env（只在 Development，見 LocalDotEnv）
builder.AddLocalDotEnv();

// --- 資料庫（連線字串來源：容器 = .env 經 compose 的 environment；本機 = .env 經 LocalDotEnv）---
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "ConnectionStrings:DefaultConnection 未設定。本機開發請在根目錄的 .env 設定 APP_DB_PASSWORD（照 .env.example），容器請確認 .env 與 compose 的 environment。");
}
// 資料表、欄位、索引一律 snake_case（PostgreSQL 慣例：未加引號的識別字會轉小寫，PascalCase 手寫 SQL 時都要加雙引號）
builder.Services.AddScoped<AuditInterceptor>();
builder.Services.AddDbContext<AppDbContext>((services, options) =>
    options.UseNpgsql(connectionString)
        .UseSnakeCaseNamingConvention()
        .AddInterceptors(services.GetRequiredService<AuditInterceptor>()));

// --- 認證（JWT；Secret 空值或不足 32 字元直接啟動失敗，不帶病上線）---
// HS256 金鑰至少 256 bits，太短要到簽發 token（登入）時才報錯，提前到啟動時擋
var jwtSecret = builder.Configuration["Jwt:Secret"];
if (string.IsNullOrWhiteSpace(jwtSecret) || jwtSecret.Length < 32)
{
    throw new InvalidOperationException("Jwt:Secret 未設定或長度不足（至少 32 字元的隨機字串，設定在 .env 的 JWT_SECRET）。");
}
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ClockSkew = TimeSpan.FromMinutes(1),
        };
    });
builder.Services.AddAuthorization();

// --- 應用服務與 endpoint/handler 註冊 ---
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<IPasswordHasher, PasswordHasher>();
builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddHandlers();
builder.Services.AddRepositories();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>();   // /health 同時驗證資料庫連線
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// --- JSON：數字欄位只收 JSON 數字 ---
// Web 預設 AllowReadingFromString 也收 "5"，OpenAPI 會因此把整數欄位標成 integer|string；
// 前端一律送 number，改 Strict 讓契約與文件一致。
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.NumberHandling = JsonNumberHandling.Strict);
// 請求格式錯誤一律丟 BadHttpRequestException，交給 GlobalExceptionHandler 回 400
// （預設只有開發環境會丟；正式環境由框架直接回沒有內容的 400，兩邊回應不一致）
builder.Services.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = true);

// --- OpenAPI / Scalar（僅開發環境掛 UI）---
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();               // /openapi/v1.json
    app.MapScalarApiReference();    // /scalar
}

app.UseAuthentication();
app.UseAuthorization();

app.MapEndpoints();
app.MapHealthChecks("/health");

// 套用 EF Migrations 並匯入種子資料
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await DataSeeder.SeedAsync(db);
}

app.Run();
