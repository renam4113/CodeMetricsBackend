using Microsoft.EntityFrameworkCore;
using CodeMetrics.Context;
using CodeMetrics.Service;
using CodeMetrics.Clients;
using CodeMetrics.Options;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;

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

builder.Services.AddScoped<ICodeMetricsService, CodeMetricsService>();
builder.Services.AddScoped<ICodeMetricsDatabaseService, CodeMetricsDatabaseService>();

builder.Services.Configure<GiteaOptions>(builder.Configuration.GetSection("Gitea"));

builder.Services.AddHttpClient<GiteaClient>((sp, client) =>
{
    var options = sp.GetRequiredService<IOptions<GiteaOptions>>().Value;

    if (string.IsNullOrWhiteSpace(options.BaseUrl))
    {
        throw new InvalidOperationException("Gitea:BaseUrl is not configured.");
    }

    client.BaseAddress = new Uri(options.BaseUrl);

    if (!string.IsNullOrWhiteSpace(options.Token))
    {
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("token", options.Token);
    }
});

builder.Services.AddDbContext<CodeMetricsDbContext>(options =>
{
    options
    .LogTo(Console.WriteLine)
    .UseNpgsql("UserName=myuser;Password=mypassword;Host=45.144.52.95;Port=5432;Database=mydatabase;");
});


builder.Services.AddControllers();
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
    await dbContext.Database.EnsureDeletedAsync();
    await dbContext.Database.EnsureCreatedAsync();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

app.Run();