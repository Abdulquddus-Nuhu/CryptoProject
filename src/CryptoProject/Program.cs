using CryptoProject.Data;
using CryptoProject.Entities.Identity;
using CryptoProject.Middlewares;
using CryptoProject.SeedDatabase;
using CryptoProject.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Events;
using System.Text;
using System.Threading.RateLimiting;

Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateBootstrapLogger();
Log.Information($"Starting up Crypto Web Server!");

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Add services to the container.
    if (builder.Environment.IsProduction())
    {
        builder.WebHost.UseUrls("http://localhost:4002");
    }
    else if (builder.Environment.IsStaging())
    {
        builder.WebHost.UseUrls("http://localhost:4001");
    }



    builder.Logging.ClearProviders();

    if (builder.Environment.IsDevelopment())
    {
        Log.Logger = new LoggerConfiguration()
           .MinimumLevel.Debug()
           .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
           .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Information)
           .Enrich.FromLogContext()
           .WriteTo.Console()
           //.WriteTo.File(outputTemplate:"", formatter: "Serilog.Formatting.Json.JsonFormatter, Serilog")
           .CreateLogger();
    }
    else if (builder.Environment.IsStaging())
    {
        Log.Logger = new LoggerConfiguration()
           .MinimumLevel.Information()
           .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
           .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
           .Enrich.FromLogContext()
           .WriteTo.Console()
           .WriteTo.File(
                @"/logs/CryptoAPIStaging/logs.txt",
                fileSizeLimitBytes: 10485760,
                rollOnFileSizeLimit: true,
                shared: true,
                retainedFileCountLimit: null,
                flushToDiskInterval: TimeSpan.FromSeconds(1))
           //.WriteTo.ApplicationInsights(TelemetryConfiguration.CreateDefault(), TelemetryConverter.Traces)
           .CreateLogger();
    }
    else
    {
        Log.Logger = new LoggerConfiguration()
           .MinimumLevel.Information()
           .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
           .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
           .Enrich.FromLogContext()
           .WriteTo.Console()
           .WriteTo.File(
                @"/logs/CryptoAPIProduction/logs.txt",
                fileSizeLimitBytes: 10485760,
                rollOnFileSizeLimit: true,
                shared: true,
                retainedFileCountLimit: null,
                flushToDiskInterval: TimeSpan.FromSeconds(1))
           //.WriteTo.ApplicationInsights(TelemetryConfiguration.CreateDefault(), TelemetryConverter.Traces)
           .CreateLogger();
    }

    builder.Host.UseSerilog();

    builder.Services.AddControllers();
    // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    string connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION") ?? string.Empty;
    builder.Services.AddDbContext<AppDbContext>(options =>
    {
        //options.UseNpgsql(connectionString, b => b.MigrationsAssembly("Infrastructure"));
        options.UseNpgsql(connectionString);
    });


    builder.Services.AddIdentity<User, Role>(
               options =>
               {
                   options.Password.RequireDigit = true;
                   options.Password.RequireNonAlphanumeric = true;
                   options.Password.RequireLowercase = true;
                   options.Password.RequireUppercase = true;
                   options.Password.RequiredLength = 8;
                   options.User.RequireUniqueEmail = true;
                   options.SignIn.RequireConfirmedEmail = true;
               })
               .AddEntityFrameworkStores<AppDbContext>()
               .AddDefaultTokenProviders();

    builder.Services.AddScoped<TokenService>();
    builder.Services.AddScoped<EmailService>();
    builder.Services.AddScoped<OtpGenerator>();
    builder.Services.AddScoped<EmailService>(provider =>
    {
        return new EmailService(builder.Environment,
            builder.Configuration,
            provider.GetRequiredService<ILogger<EmailService>>()
        );
    });


    // Register the worker responsible of seeding the database.
    builder.Services.AddHostedService<SeedDb>();


    var key = Encoding.ASCII.GetBytes(Environment.GetEnvironmentVariable("JWT_SECRET_KEY") ?? string.Empty);
    var tokenValidationParams = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        RequireExpirationTime = true,
        ClockSkew = TimeSpan.Zero
    };

    builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(config =>
    {
        config.RequireHttpsMetadata = false;
        config.SaveToken = true;
        config.TokenValidationParameters = tokenValidationParams;
    });
    //builder.Services.AddAuthorization(options =>
    //{
    //    options.AddPolicy(AuthConstants.Policies.ADMINS, policy => policy.RequireRole(AuthConstants.Roles.ADMIN, AuthConstants.Roles.SUPER_ADMIN));
    //});
    
    builder.Services.AddAuthorization();

    //Ensure all controllers use jwt token
    //builder.Services.AddControllers(options =>
    //{
    //    var policy = new AuthorizationPolicyBuilder()
    //        .RequireAuthenticatedUser()
    //        .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
    //        .Build();
    //    options.Filters.Add(new AuthorizeFilter(policy));
    //});

    builder.Services.AddCors(options =>
    {
        options.AddPolicy(name: "MyAllowSpecificOrigins",
                          builder =>
                          {
                              builder
                              .WithOrigins("https://localhost:3000", "http://localhost:3000",
                                "https://www.bps-ca.com", "http://www.bps-ca.com")
                                .AllowAnyHeader()
                                .AllowAnyMethod();
                          });
    });


    //Swagger Authentication/Authorization
    builder.Services.AddSwaggerGen(c =>
    {
        var securityScheme = new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "JWT Authorization header using the Bearer scheme. **Enter Bearer Token Only**",
            Reference = new OpenApiReference
            {
                Id = "Bearer",
                Type = ReferenceType.SecurityScheme
            }
        };

        c.EnableAnnotations();
        c.AddSecurityDefinition(securityScheme.Reference.Id, securityScheme);
        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            { securityScheme, Array.Empty<string>() }
        });
    });

    // Security and Production enhancements 
    if (!builder.Environment.IsDevelopment())
    {
        // Proxy Server Config
        builder.Services.Configure<ForwardedHeadersOptions>(
              options =>
              {
                  options.ForwardedHeaders =
                      ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
              });

        //Persist key
        builder.Services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo("/var/keys"));
    }

    //Remove Server Header
    builder.WebHost.UseKestrel(options => options.AddServerHeader = false);


    builder.Services.AddRateLimiter(_ => _
    .AddFixedWindowLimiter(policyName: "fixed", options =>
    {
        options.PermitLimit = 4; // Maximum requests allowed
        options.Window = TimeSpan.FromSeconds(12); // Time window duration
        options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        options.QueueLimit = 2; // Maximum queued requests
    }));

    builder.Services.AddResponseCaching();

    //builder.Services.AddHttpClient();

    //builder.Services.AddOpenTelemetry().WithTracing(b =>
    //{
    //    b
    //    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(builder.Environment.EnvironmentName))
    //    .AddAspNetCoreInstrumentation()
    //    .AddOtlpExporter(opt => { opt.Endpoint = new Uri("http://localhost:4317"); });
    //});

    var app = builder.Build();

    //Configure the HTTP request pipeline.
    if (!app.Environment.IsProduction())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseRateLimiter();
    app.UseResponseCaching();

    //security
    app.UseMiddleware<UserAgentValidationMiddleware>();
    //app.UseMiddleware<NotFoundRequestTrackingMiddleware>();


    app.UseHsts();
    app.UseHttpsRedirection();

    app.UseForwardedHeaders(new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
    });

    app.UseCors("MyAllowSpecificOrigins");

    app.UseAuthentication();
    app.UseAuthorization();



    app.MapControllers();

    app.Run();

}
catch (Exception ex)
{
    Log.Fatal(ex, "An unhandled exception occurred during bootstrapping the Server!");
}
finally
{
    Log.CloseAndFlush();
}

