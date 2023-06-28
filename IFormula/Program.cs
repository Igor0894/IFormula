using ApplicationServices.Scheduller;
using Quartz;
using NLog;
using NLog.Web;
using ApplicationServices.Services;
using Quartz.Spi;
using Quartz.Impl;
using IFormula.Middlewares;
using TSDBWorkerAPI.Models;
using TSDBWorkerAPI;

var logger = LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Logging.ClearProviders();
builder.Host.UseNLog();
builder.Services.AddTransient<ExceptionsMiddleware>();
builder.Services.AddSingleton<IJobFactory,JobFactory>();
builder.Services.AddSingleton<CalcServiceCollector>();
builder.Services.AddSingleton<ISchedulerFactory, StdSchedulerFactory>();
builder.Services.AddSingleton<ManageNodeService>();
builder.Services.AddSingleton(w => new TsdbClient(builder.Configuration.GetSection("TsdbSettings").Get<TsdbSettings>()));
builder.Services.AddHostedService<QuartzHostedService>();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyHeader();
        policy.AllowAnyMethod();
        policy.AllowAnyOrigin();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment()) { }
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseCors();

app.UseAuthorization();

app.MapControllers();

app.UseMiddleware<ExceptionsMiddleware>();

app.Logger.LogInformation("Application started\r\n");

app.Run();
