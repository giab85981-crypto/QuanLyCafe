using System.Text;
using CafeManagement.API.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

var databaseConnection = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Thiếu ConnectionStrings:DefaultConnection.");
if (builder.Environment.IsDevelopment() && builder.Configuration.GetValue<bool>("LocalDb:UseNamedPipe"))
    databaseConnection = await DevelopmentLocalDb.ResolveAsync(databaseConnection);

// 1. Cấu hình DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(databaseConnection));

// 2. Cấu hình CORS Policy: AllowReactApp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.SetIsOriginAllowed(_ => true) // Cho phép tất cả Domain/Port (bao gồm http://localhost:5173)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// 3. Cấu hình JWT Authentication
var jwtSettings = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSettings["Key"] ?? "CafeManagement_Super_Secret_Key_2026_For_Security_123456789!";
var key = Encoding.UTF8.GetBytes(jwtKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.Events = new JwtBearerEvents {
        OnTokenValidated = async context => {
            var db = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
            var username = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var account = await db.Accounts.AsNoTracking().Include(a => a.Role).FirstOrDefaultAsync(a => a.UserName == username);
            var version = context.Principal?.FindFirst("SecurityVersion")?.Value ?? "0";
            if (account == null || !account.IsActive || account.SecurityVersion.ToString() != version) { context.Fail("Tài khoản đã khóa hoặc phiên đăng nhập đã hết hiệu lực."); return; }
            var identity = (System.Security.Claims.ClaimsIdentity)context.Principal!.Identity!;
            foreach (var claim in identity.FindAll(System.Security.Claims.ClaimTypes.Role).ToList()) identity.RemoveClaim(claim);
            identity.AddClaim(new(System.Security.Claims.ClaimTypes.Role, account.Role!.Name));
            foreach (var claim in identity.FindAll("Permission").ToList()) identity.RemoveClaim(claim);
            foreach (var code in await CafeManagement.API.Services.DynamicAccess.Effective(db, account.UserName)) identity.AddClaim(new("Permission", code));
        }
    };
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidateAudience = true,
        ValidAudience = jwtSettings["Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddControllers(options => options.Filters.Add<CafeManagement.API.Services.StaffAccessFilter>());
builder.Services.AddEndpointsApiExplorer();

// 4. Cấu hình SwaggerGen hỗ trợ gửi Bearer Token
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Cafe Management API",
        Version = "v1"
    });

    // Thêm nút Authorize cho JWT Bearer Token trên Swagger UI
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Nhập trực tiếp chuỗi Token JWT thu được từ API Login vào ô bên dưới."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});
var app = builder.Build();

// 5. Cấu hình Middleware Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Kích hoạt Middleware theo đúng thứ tự bắt buộc
app.UseCors("AllowReactApp");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// 6. Tự động khởi tạo CSDL và Seed dữ liệu mẫu
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        await DbSeeder.SeedAsync(context);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Lỗi xảy ra trong quá trình Seed dữ liệu mẫu!");
    }
}

app.Run();
