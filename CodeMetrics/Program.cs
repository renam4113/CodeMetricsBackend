using Microsoft.EntityFrameworkCore;
using CodeMetrics.Context;
using Microsoft.EntityFrameworkCore.Design;
using static CodeMetrics.Context.CodeMetricsDbContext;
using Npgsql.PostgresTypes;
using CodeMetrics.Service;

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

builder.Services.AddDbContext<CodeMetricsDbContext>(options =>
{
    options.UseNpgsql("UserName=postgres;Password=LFYSGY0UpphAliOEgqpv;Host=localhost;Port=5434;Database=filedb;");
    //options.UseNpgsql("UserName=myuser;Password=mypassword;Host=45.144.52.95;Port=5432;Database=mydatabase;");
});

// Add services to the container.
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
    await dbContext.Database.MigrateAsync();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

app.Run();