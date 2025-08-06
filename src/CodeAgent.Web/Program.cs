using CodeAgent.Core.Services;
using CodeAgent.Domain.Interfaces;
using CodeAgent.Infrastructure.Services;
using CodeAgent.Web.Hubs;
using CodeAgent.Web.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add SignalR for real-time communication
builder.Services.AddSignalR();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? "default-security-key"))
        };
    });

builder.Services.AddAuthorization();

// Register application services
builder.Services.AddSingleton<IFileSystemService, FileSystemService>();
builder.Services.AddSingleton<IGitService, GitService>();
builder.Services.AddSingleton<ICacheService, CacheService>();
builder.Services.AddSingleton<ISessionService, SessionService>();
builder.Services.AddSingleton<ISecurityService, SecurityService>();
builder.Services.AddSingleton<IAuditService, AuditService>();
builder.Services.AddSingleton<IDlpService, DlpService>();
builder.Services.AddSingleton<IThreatDetectionService, ThreatDetectionService>();
builder.Services.AddSingleton<ISandboxService, SandboxService>();
builder.Services.AddSingleton<ProviderManager>();
builder.Services.AddSingleton<ContextManager>();
builder.Services.AddSingleton<PluginManager>();
builder.Services.AddSingleton<PerformanceMonitor>();

// Add web-specific services
builder.Services.AddSingleton<IAnalyticsService, AnalyticsService>();
builder.Services.AddSingleton<ITeamService, TeamService>();
builder.Services.AddSingleton<IExportService, ExportService>();
builder.Services.AddSingleton<ITelemetryService, TelemetryService>();

// Add hosted services
builder.Services.AddHostedService<MetricsCollectorService>();
builder.Services.AddHostedService<CleanupService>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<AgentHub>("/agenthub");
app.MapHub<CollaborationHub>("/collaborationhub");

// Serve static files (for web UI)
app.UseDefaultFiles();
app.UseStaticFiles();

app.Run();