using System.IO.Compression;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.RateLimiting;
using backend.Context;
using backend.Middlewares;
using backend.Services.AccountService;
using backend.Services.AnnounceService;
using backend.Services.CleanUpJobs;
using backend.Services.CloudFlareCdnServices;
using backend.Services.Helpers;
using backend.Services.LocationService;
using backend.Services.QualityService;
using backend.UOW;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Quartz;


// setting env vars
Environment.SetEnvironmentVariable("CDN_API_KEY", "vQLhTnTBJoanfWOLxd7HE1vktX2bjE1V_KXdDovs");
Environment.SetEnvironmentVariable("CDN_STORE_ID" , "753a27c009be43e4ae5b3d28d950e036");
Environment.SetEnvironmentVariable("CDN_ACCOUNT_ID" , "f1c76dd34830b0089823f17710dbc523");
Environment.SetEnvironmentVariable("CDN_S3_OPERATIONS" , "https://f1c76dd34830b0089823f17710dbc523.r2.cloudflarestorage.com/images");
Environment.SetEnvironmentVariable("CDN_ACCESS_KEY_ID" , "3d7be1a5b3a500615fe4ef73203d9dc3");
Environment.SetEnvironmentVariable("CDN_SECRET_KEY_ID" , "6a37c0db948016b33b6596a731c088c0678b42490ba591a6d31dab72e3fd01d0");

var builder = WebApplication.CreateBuilder(args);

var isDocker = Environment.GetEnvironmentVariable("DOCKER") == "true";

builder.Services.AddResponseCompression(options =>
{
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        new[] {
            "application/json",
            "application/javascript",
            "application/xml",
            "text/css",
            "application/octet-stream",
            "image/svg+xml",
            "application/x-font-ttf",
            "application/vnd.ms-fontobject",
            "font/woff",
            "font/woff2"
        });
});

builder.Services.Configure<BrotliCompressionProviderOptions>(options =>
{
    options.Level = CompressionLevel.Optimal; // Choose Fastest or Optimal
});
builder.Services.Configure<GzipCompressionProviderOptions>(options =>
{
    options.Level = CompressionLevel.Fastest; // Choose Fastest or Optimal
});

builder.Services.AddHealthChecks();

// var getDockerEnv = Environment.GetEnvironmentVariable("DOCKER");

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi


builder.Services.AddOpenApi();
builder.Services.AddControllers();
var connString = "";
if (isDocker)
{
    Console.WriteLine("✅ Running in Docker mode. Building connection string from environment variables.");
    // Read the environment variables provided by Docker Compose
    var dbHost = Environment.GetEnvironmentVariable("DB_HOST");
    var dbPort = Environment.GetEnvironmentVariable("DB_PORT");
    var dbUser = Environment.GetEnvironmentVariable("DB_USER");
    var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD");
    var dbName = Environment.GetEnvironmentVariable("DB_NAME");

    // Build the connection string for MySQL/MariaDB
    connString = $"Server={dbHost};Port={dbPort};Database={dbName};User={dbUser};Password={dbPassword};";
}
else
{
    Console.WriteLine("✅ Running in local mode.");
    connString = builder.Configuration.GetConnectionString("ConnString");
}

if (string.IsNullOrEmpty(connString))
{
    throw new InvalidOperationException("Database connection string ('ConnString') is not configured.");
}

builder.Services.AddQuartz(q =>
{
    q.UsePersistentStore(opt =>
    {
        opt.UseProperties = true;
        opt.UseMySql(connString!);
        opt.UseSystemTextJsonSerializer();
        opt.PerformSchemaValidation = true;
    });

    var sessionTokenCleanUpJobKey = JobKey.Create("session-token-clean-up-job", "session-tokens");
    var announceCleanUpJobKey = JobKey.Create("announces-cleanup-job", "announces");
    
    q.AddJob<SessionTokenCleanUp>(sessionTokenCleanUpJobKey)
        .AddTrigger(trigger =>
        {
            trigger.ForJob(sessionTokenCleanUpJobKey)
                .WithIdentity("session-token-clean-up-trigger", "session-token-trigger")
                .WithCronSchedule("0 0 */12 ? * *")
                .StartNow();
        });
    
    q.AddJob<AnnouncesCleanUp>(announceCleanUpJobKey)
        .AddTrigger(trigger =>
        {
            trigger.ForJob(announceCleanUpJobKey)
                .WithIdentity("announces-clean-up-trigger", "announces-trigger")
                .WithCronSchedule("0 0 */12 ? * *")
                .StartNow();
        });
});

builder.Services.AddQuartzHostedService(opt =>
{
    opt.WaitForJobsToComplete = true;
});

builder.Services.AddDbContext<DbContextBrainlyPhysio>(options =>
{
    options.UseMySql(connString, ServerVersion.AutoDetect(connString))
        .UseLoggerFactory(LoggerFactory.Create(opt =>
        {
            opt.AddJsonConsole(option =>
            {
                option.IncludeScopes = true;
                option.TimestampFormat = "[yyyy-MM-dd HH:mm:ss] ";
                option.JsonWriterOptions = new System.Text.Json.JsonWriterOptions
                {
                    Indented = true,
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                };

            });

        }));
});

// CORS configuration
var corsPolicy = new[]
{
    "http://31.97.183.12:3000",
    "https://31.97.183.12:3000",
    "http://localhost:3000"
};

// var corsPolicy = "http://localhost:3000";

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontEnd",
        policy =>
        {
            policy.WithOrigins(corsPolicy)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
});

// Rate limiting
builder.Services.AddRateLimiter(x =>
    x.AddFixedWindowLimiter(policyName: "fixed", options =>
    {
        options.PermitLimit = 4; // 4 requests
        options.Window = TimeSpan.FromSeconds(30);
        options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        options.QueueLimit = 2;
    }));

// JWT 

var jwtSettings = builder.Configuration.GetSection("JwtSettings");

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IAnnounceService,AnnounceService>();
builder.Services.AddScoped<ILocationService,LocationService>();
builder.Services.AddScoped<IQualityService,QualityService>();
builder.Services.AddScoped<ICloudFlareCdnService,CloudFlareCdnService>();
builder.Services.AddTransient<JwtTokenMiddleware>();
builder.Services.AddTransient<AdminMiddleware>();
builder.Services.AddSingleton<UserHelpers>();

var key = await CloudFlareCdnService.GetCdnSecret("jwt",true);
builder.Services.AddAuthentication(x =>
    {
        x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        x.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(x =>
    {
        x.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                context.Token = context.Request.Cookies["JWTToken"];
                return Task.CompletedTask;
            }
        };

        x.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.Zero
        };
    });

// Authorization policy
builder.Services.AddAuthorization();

var app = builder.Build();

app.UseCors("AllowFrontEnd");
app.MapHealthChecks("/healthz").AllowAnonymous();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.UseSwaggerUI(opt =>
    {
        opt.SwaggerEndpoint("/openapi/v1.json" , "BrainlyPhysio v1");
        opt.RoutePrefix = "api-docs";
    });
}

// app.UseHttpsRedirection();
// app.UseMiddleware<AdminMiddleware>();
// app.UseAuthentication();
// app.UseAuthorization();
//
// app.Run();

app.UseRateLimiter();
app.UseMiddleware<JwtTokenMiddleware>();
app.UseMiddleware<AdminMiddleware>();
app.UseHttpsRedirection();
app.UseResponseCompression();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();
