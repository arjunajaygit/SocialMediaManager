using Microsoft.EntityFrameworkCore;
using System.Threading.RateLimiting;
using SocialMediaManager.Infrastructure.Data;
using SocialMediaManager.Application.Interfaces;
using SocialMediaManager.Application.Services;
using Microsoft.AspNetCore.RateLimiting;
using Hangfire;
using Hangfire.PostgreSql;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors();
builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("StandardPolicy", opt =>
    {
        opt.PermitLimit = 60;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 2;
    });

    options.AddFixedWindowLimiter("AIPolicy", opt =>
    {
        opt.PermitLimit = 5;
        opt.Window = TimeSpan.FromMinutes(1);
    });
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IApplicationDbContext>(provider => 
    provider.GetRequiredService<AppDbContext>());

builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UsePostgreSqlStorage(options => 
        options.UseNpgsqlConnection(builder.Configuration.GetConnectionString("DefaultConnection"))));

builder.Services.AddHangfireServer();

builder.Services.AddScoped<ISocialMediaProvider, SocialMediaManager.Infrastructure.Services.LinkedInProvider>();
builder.Services.AddScoped<IPostService, PostService>();
builder.Services.AddSingleton<IEncryptionService, EncryptionService>(); 
builder.Services.AddScoped<AccountService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddHttpClient<GeminiService>();
builder.Services.AddScoped<MediaService>();
builder.Services.AddScoped<SocialMediaManager.API.Jobs.PublishScheduledPostsJob>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseRouting();

app.UseRateLimiter();

app.UseCors(policy => policy
    .AllowAnyHeader()
    .AllowAnyMethod()
    .SetIsOriginAllowed(origin => true) 
    .AllowCredentials());

app.UseAuthorization();

app.UseHangfireDashboard();
app.MapControllers();

RecurringJob.AddOrUpdate<SocialMediaManager.API.Jobs.PublishScheduledPostsJob>(
    "publish-scheduled-posts",
    job => job.ExecuteAsync(),
    Cron.Minutely
);

app.Run();