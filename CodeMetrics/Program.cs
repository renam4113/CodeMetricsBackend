using Microsoft.EntityFrameworkCore;
using CodeMetrics.Context;
using CodeMetrics.Service;
using CodeMetrics.Clients;
using CodeMetrics.Options;

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
builder.Configuration.AddJsonFile("appsettings.json");

var options = builder.Configuration.GetSection("Gitea").Get<GiteaOptions>();
builder.Services.AddSingleton(options);
builder.Services.AddHttpClient<GiteaClient>();

builder.Services.AddDbContext<CodeMetricsDbContext>(options =>
{
    options
    .LogTo(Console.WriteLine)
    .UseNpgsql("Username=postgres;Password=LFYSGY0UpphAliOEgqpv;Host=195.208.118.188;Port=5434;Database=filedb;");
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
    //await dbContext.Database.EnsureDeletedAsync();
    await dbContext.Database.EnsureCreatedAsync();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

app.Run();