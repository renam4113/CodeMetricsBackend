using CodeMetrics.Application.Contracts;
using CodeMetrics.Clients;
using CodeMetrics.Context;
using CodeMetrics.Infrastructure.Filters;
using CodeMetrics.Infrastructure.Ollama;
using CodeMetrics.Models;
using CodeMetrics.Options;
using CodeMetrics.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Configuration.AddJsonFile("appsettings.json");

builder.Services.Configure<OllamaOptions>(builder.Configuration.GetSection(OllamaOptions.SectionName));


builder.Services.Configure<SonarQubeSettings>(builder.Configuration.GetSection("SonarQube"));

builder.Services.AddHttpClient<ISonarQubeService, SonarQubeService>();

// ??? ???? ?? ??????????? IHttpClientFactory:

builder.Services.AddScoped<ICodeMetricsService, CodeMetricsService>();
builder.Services.AddScoped<ICodeMetricsDatabaseService, CodeMetricsDatabaseService>();
builder.Services.AddSingleton<IOllamaService, OllamaService>();
builder.Services.AddScoped<ISonarQubeService, SonarQubeService>(); 

var giteaOptions = builder.Configuration.GetSection("Gitea").Get<GiteaOptions>()
    ?? throw new InvalidOperationException("Gitea configuration section is missing.");
builder.Services.AddSingleton(giteaOptions);
builder.Services.AddHttpClient<GiteaClient>();

var ollamaBaseUrl = builder.Configuration["Ollama:BaseUrl"] ?? "http://localhost:11434";
builder.Services.AddHttpClient("Ollama", client =>
{
    client.BaseAddress = new Uri(ollamaBaseUrl);
    client.Timeout = TimeSpan.FromMinutes(6);
});

var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("Connection string 'Default' is not configured.");

builder.Services.AddDbContext<CodeMetricsDbContext>(options =>
{
    options.LogTo(Console.WriteLine).UseNpgsql(connectionString);
});

builder.Services.AddScoped<GlobalExceptionFilter>();
builder.Services.AddControllers(options =>
{
    options.Filters.Add<GlobalExceptionFilter>();
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<CodeMetricsDbContext>();
    await dbContext.Database.EnsureCreatedAsync();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

app.Run();
