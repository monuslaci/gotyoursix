using Microsoft.Extensions.Logging;
using gotyoursix.Data;
using static gotyoursix.Data.DBContext;
using gotyoursix.Services;
using gotyoursix.Components;
using gotyoursix.Helpers;
using Microsoft.AspNetCore.Components.Authorization;
using AspNetCore.Identity.MongoDbCore.Infrastructure;
using AspNetCore.Identity.MongoDbCore.Models;
using AspNetCore.Identity.MongoDbCore.Extensions;
using Microsoft.AspNetCore.Identity;
using MongoDB.Driver;
using MongoDbGenericRepository.Attributes;
using Blazored.Toast;
using SendGrid;
using SendGrid.Helpers.Mail;
using Microsoft.Identity.Client;
using Microsoft.Extensions.Configuration;

namespace gotyoursix;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        builder.Services.AddMauiBlazorWebView();
        DotNetEnv.Env.Load();
        var config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        builder.Services.AddBlazoredToast();
        builder.Services.AddSingleton<WeatherForecastService>();

        // Register password hasher
        builder.Services.AddSingleton<IPasswordHasher<Users>, PasswordHasher<Users>>();

        // Get MongoDB connection info
        //var connectionString = Environment.GetEnvironmentVariable("MONGODB_CONNECTION_STRING");
        var connectionString = config["ConnectionStrings:MONGODB_CONNECTION_STRING"];
        //var databaseName = Environment.GetEnvironmentVariable("DB_NAME");
        var databaseName = config["ConnectionStrings:DB_NAME"];

        // MongoDB Identity Configuration
        var identityConfig = new MongoDbIdentityConfiguration
        {
            MongoDbSettings = new MongoDbSettings
            {
                ConnectionString = connectionString,
                DatabaseName = databaseName
            },
            IdentityOptionsAction = options =>
            {
                options.Password.RequiredLength = 6;
                options.Password.RequireDigit = false;
                options.Password.RequireUppercase = false;
            }
        };

        // Configure Mongo Identity
        //builder.Services.ConfigureMongoDbIdentity<Users, ApplicationRole, string>(identityConfig)
        //    .   AddDefaultTokenProviders();
        builder.Services.ConfigureMongoDbIdentity<Users, ApplicationRole, string>(identityConfig);


        // Register Mongo client
        builder.Services.AddSingleton<IMongoDatabase>(sp =>
        {
            var client = new MongoClient(connectionString);
            return client.GetDatabase(databaseName);
        });

        // Register Auth Services
        builder.Services.AddScoped<CustomAuthenticationStateProvider>();
        builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<CustomAuthenticationStateProvider>());
        builder.Services.AddScoped<IEmailService, EmailService>();

        var pca = PublicClientApplicationBuilder.Create("1d530638-1a3d-4204-8035-aa830b84b4f6")
                .WithRedirectUri("gotyoursix://auth")
                .Build();

        builder.Services.AddSingleton(pca);

        // Authorization
        builder.Services.AddAuthorizationCore();

        // SendGrid
        //var sendGridKey = Environment.GetEnvironmentVariable("SENDGRID_API_KEY");
        var sendGridKey = config["SendGrid:ApiKey"];
        builder.Services.AddSingleton<ISendGridClient>(new SendGridClient(sendGridKey));

        return builder.Build();
    }
}
