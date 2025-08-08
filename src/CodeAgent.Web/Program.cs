using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using CodeAgent.Core.Services;
using CodeAgent.Domain.Interfaces;
using CodeAgent.Infrastructure.Services;
using CodeAgent.Web.Services;
using CodeAgent.Providers.OpenAI;
using CodeAgent.Providers.Claude;
using CodeAgent.Providers.Ollama;
using CodeAgent.Providers.Docker;
using CodeAgent.MCP;
using CodeAgent.Web.Hubs;
using Microsoft.Extensions.Options;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.SpaServices.AngularCli;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel based on environment
builder.WebHost.ConfigureKestrel(options =>
{
    // In Docker or when ASPNETCORE_URLS is set, listen on all interfaces
    var urls = Environment.GetEnvironmentVariable("ASPNETCORE_URLS");
    var isDocker = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";
    
    if (isDocker || !string.IsNullOrEmpty(urls))
    {
        options.ListenAnyIP(5001);
    }
    else
    {
        options.ListenLocalhost(5001);
    }
});

// Set up configuration paths
var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
var codeAgentDir = Path.Combine(homeDir, ".codeagent");
Directory.CreateDirectory(codeAgentDir);
var settingsPath = Path.Combine(codeAgentDir, "settings.json");
var yamlSettingsPath = Path.Combine(codeAgentDir, "settings.yaml");

// Build configuration with file-based settings
builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true)
    .AddJsonFile(settingsPath, optional: true, reloadOnChange: true)
    .AddYamlFile(yamlSettingsPath, optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add CSRF protection
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.SuppressXFrameOptionsHeader = false;
});

// Add SignalR
builder.Services.AddSignalR();

// Add SPA static files
builder.Services.AddSpaStaticFiles(configuration =>
{
    configuration.RootPath = "wwwroot/browser";
});

// Add CORS for localhost and Docker environments
builder.Services.AddCors(options =>
{
    options.AddPolicy("LocalhostOnly", policy =>
    {
        policy.WithOrigins(
                "http://localhost:5001", 
                "http://localhost:4200",
                "http://localhost:8080",  // Docker with nginx
                "http://127.0.0.1:5001",
                "http://127.0.0.1:8080",
                "http://host.docker.internal:5001" // Docker internal
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
            .SetIsOriginAllowed(origin => 
            {
                // Allow any localhost or Docker internal origin
                var uri = new Uri(origin);
                return uri.Host == "localhost" || 
                       uri.Host == "127.0.0.1" || 
                       uri.Host.Contains("docker.internal") ||
                       uri.Host == "codeagent"; // Docker service name
            });
    });
});

// Add session services for persistence
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = ".CodeAgent.Session";
    options.IdleTimeout = TimeSpan.FromHours(24);
    options.Cookie.IsEssential = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

// Configure logging
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.SetMinimumLevel(LogLevel.Information);
});

// Configure HttpClient
builder.Services.AddHttpClient("OllamaClient", client =>
{
    client.Timeout = TimeSpan.FromMinutes(5);
});
builder.Services.AddHttpClient();

// Core services
builder.Services.AddSingleton<IPermissionPrompt, WebPermissionPrompt>();
builder.Services.AddSingleton<IConfigurationService, ConfigurationService>();
builder.Services.AddSingleton<IFileSystemService, FileSystemService>();
builder.Services.AddSingleton<IDiffService, DiffService>();
builder.Services.AddSingleton<IContextService, ContextService>();
builder.Services.AddSingleton<IRetryService, RetryService>();
builder.Services.AddSingleton<IPermissionService, PermissionService>();
builder.Services.AddSingleton<IInternalToolService, InternalToolService>();
builder.Services.AddSingleton<ChatService>();
builder.Services.AddSingleton<IChatService>(sp => sp.GetRequiredService<ChatService>());

// Additional services for web
builder.Services.AddScoped<ProviderManager>();
builder.Services.AddScoped<IProviderManager>(sp => sp.GetRequiredService<ProviderManager>());
builder.Services.AddScoped<ISessionService, SessionService>();
builder.Services.AddScoped<ISecurityService, SecurityService>();

// LLM provider configuration
builder.Services.Configure<OpenAIOptions>(builder.Configuration.GetSection("OpenAI"));
builder.Services.Configure<ClaudeOptions>(builder.Configuration.GetSection("Claude"));
builder.Services.Configure<OllamaOptions>(builder.Configuration.GetSection("Ollama"));
builder.Services.Configure<DockerLLMOptions>(builder.Configuration.GetSection("DockerLLM"));
builder.Services.Configure<DockerMCPOptions>(builder.Configuration.GetSection("DockerMCP"));
builder.Services.Configure<MCPOptions>(builder.Configuration.GetSection("MCP"));

// LLM providers
builder.Services.AddSingleton<OpenAIProvider>();
builder.Services.AddSingleton<ClaudeProvider>();
builder.Services.AddSingleton<OllamaProvider>(sp =>
{
    var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
    var httpClient = httpClientFactory.CreateClient("OllamaClient");
    var options = sp.GetRequiredService<IOptions<OllamaOptions>>();
    var logger = sp.GetRequiredService<ILogger<OllamaProvider>>();
    return new OllamaProvider(options, logger, httpClient);
});

// Docker LLM provider
builder.Services.AddSingleton<DockerLLMProvider>(sp =>
{
    var httpClient = sp.GetRequiredService<HttpClient>();
    var options = sp.GetRequiredService<IOptions<DockerLLMOptions>>();
    var logger = sp.GetRequiredService<ILogger<DockerLLMProvider>>();
    return new DockerLLMProvider(options, logger, httpClient);
});

// Docker MCP provider
builder.Services.AddSingleton<DockerMCPProvider>(sp =>
{
    var httpClient = sp.GetRequiredService<HttpClient>();
    var options = sp.GetRequiredService<IOptions<DockerMCPOptions>>();
    var logger = sp.GetRequiredService<ILogger<DockerMCPProvider>>();
    return new DockerMCPProvider(logger, options, httpClient);
});

// Model management services
builder.Services.AddSingleton<OllamaModelManager>(sp =>
{
    var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
    var httpClient = httpClientFactory.CreateClient("OllamaClient");
    var options = sp.GetRequiredService<IOptions<OllamaOptions>>();
    var logger = sp.GetRequiredService<ILogger<OllamaModelManager>>();
    return new OllamaModelManager(options, logger, httpClient);
});

// MCP client
builder.Services.AddSingleton<IMCPClient, MCPClient>();

// Provider factory
builder.Services.AddSingleton<ILLMProviderFactory>(sp =>
{
    var factory = new LLMProviderFactory(sp);
    factory.RegisterProvider<OpenAIProvider>("openai");
    factory.RegisterProvider<ClaudeProvider>("claude");
    factory.RegisterProvider<OllamaProvider>("ollama");
    factory.RegisterProvider<DockerLLMProvider>("docker");
    factory.RegisterProvider<DockerLLMProvider>("docker-llm");
    return factory;
});

// Default provider
builder.Services.AddSingleton<ILLMProvider>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var configService = sp.GetRequiredService<IConfigurationService>();
    var providerName = configService.GetValue("DefaultProvider") ?? config["DefaultProvider"] ?? "openai";
    var factory = sp.GetRequiredService<ILLMProviderFactory>();
    
    try
    {
        return factory.GetProvider(providerName);
    }
    catch
    {
        // If the default provider is not available, return OpenAI as fallback
        return factory.GetProvider("openai");
    }
});

var app = builder.Build();

// Configure forwarded headers for reverse proxy support
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor | 
                       Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Enable WebSockets
app.UseWebSockets(new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromSeconds(120)
});

app.UseCors("LocalhostOnly");

// Add session middleware
app.UseSession();

// Add antiforgery middleware
app.UseAntiforgery();

app.UseRouting();
app.UseAuthorization();

// Enable static file serving from wwwroot
app.UseStaticFiles();

// Serve SPA static files from browser subfolder
app.UseSpaStaticFiles();

app.MapControllers();

// Map SignalR hubs
app.MapHub<AgentHub>("/hub/agent");
app.MapHub<CollaborationHub>("/hub/collaboration");

// Configure SPA - only if not overriding API routes
app.MapFallbackToFile("index.html", new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(builder.Environment.ContentRootPath, "wwwroot", "browser"))
});

// Auto-open browser in daemon mode
var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
lifetime.ApplicationStarted.Register(() =>
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("CodeAgent Web Portal started at http://localhost:5001");
    
    // Auto-open browser if not in headless mode
    if (!args.Contains("--headless"))
    {
        OpenBrowser("http://localhost:5001");
    }
});

app.Run();

static void OpenBrowser(string url)
{
    try
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            Process.Start("open", url);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            Process.Start("xdg-open", url);
        }
    }
    catch
    {
        // Silently ignore if browser cannot be opened
    }
}

public partial class Program { }

// Extension method to add YAML configuration
public static class ConfigurationBuilderExtensions
{
    public static IConfigurationBuilder AddYamlFile(this IConfigurationBuilder builder, string path, bool optional = false, bool reloadOnChange = false)
    {
        if (!optional && !File.Exists(path))
            return builder;
            
        if (File.Exists(path))
        {
            var yaml = File.ReadAllText(path);
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();
                
            var yamlObject = deserializer.Deserialize<Dictionary<string, object>?>(yaml);
            if (yamlObject != null)
            {
                var jsonString = System.Text.Json.JsonSerializer.Serialize(yamlObject);
                builder.AddJsonStream(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(jsonString)));
            }
        }
        
        return builder;
    }
}