using Microsoft.Data.Sqlite;
using P1LiveView.Hubs;
using P1Reader.Domain.Interfaces;
using P1Reader.Infra.Sqlite.Factories;
using P1Reader.Infra.Sqlite.Interfaces;
using P1Reader.Infra.Sqlite.Services;
using P1ReaderApp.Interfaces;
using Serilog;

namespace P1LiveView
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddScoped(s => builder.Configuration);

            builder.Services.AddScoped(sp => Log.Logger);

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            builder.Services.AddScoped<IConnectionFactory<SqliteConnection>, SqliteConnectionFactory>();
            builder.Services.AddScoped<IStorage, SqLiteStorage>();
            builder.Services.AddScoped<ITrigger<FileInfo>, OnSqliteDbRotationTrigger>();
            
            builder.Services.AddSignalR();

            var app = builder.Build();

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