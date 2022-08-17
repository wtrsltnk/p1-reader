using Microsoft.AspNetCore.Builder;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using P1LiveView.Hubs;
using P1Reader.Domain.Interfaces;
using P1Reader.Infra.Sqlite.Factories;
using P1Reader.Infra.Sqlite.Interfaces;
using P1Reader.Infra.Sqlite.Services;
using P1ReaderApp.Interfaces;
using Serilog;
using System;
using System.IO;

namespace P1LiveView
{
    public class Program
    {
        public static void Main(
            string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", true)
                .Build();

            var logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .CreateLogger();

            logger.Information("Starting up");

            var builder = WebApplication
                .CreateBuilder(args);

            builder.Host
                .UseSerilog(logger);

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            builder.Services.AddScoped<IConnectionFactory<SqliteConnection>, SqliteConnectionFactory>();
            builder.Services.AddScoped<IStorage, SqLiteStorage>();
            builder.Services.AddScoped<ITrigger<FileInfo>, OnSqliteDbRotationTrigger>();

            builder.Services.AddSignalR();

            var app = builder.Build();

            app.UseSerilogRequestLogging();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.MapHub<P1Hub>("/p1");

            app.Run();
        }
    }
}