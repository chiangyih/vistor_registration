using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VisitorReg.Application.UseCases;
using VisitorReg.Infrastructure.Data;
using VisitorReg.Infrastructure.Repositories;
using VisitorReg.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

// 資料庫設定
builder.Services.AddDbContext<VisitorDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("VisitorDb"),
        sqlOptions => sqlOptions.EnableRetryOnFailure()));

// Identity 設定
builder.Services.AddDefaultIdentity<IdentityUser>(options =>
{
    // 密碼政策
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 8;

    // 登入政策
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // 使用者政策
    options.User.RequireUniqueEmail = true;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<VisitorDbContext>();

// Repository 註冊
builder.Services.AddScoped<IVisitorRepository, VisitorRepository>();
builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();

// Service 註冊
builder.Services.AddScoped<AuditService>();

// Use Case 註冊
builder.Services.AddScoped<CreateVisitorUseCase>();
builder.Services.AddScoped<SearchVisitorsUseCase>();
builder.Services.AddScoped<CheckoutVisitorUseCase>();
builder.Services.AddScoped<GetVisitorDetailUseCase>();

// Session 設定
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Anti-forgery 設定
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseSession();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();

