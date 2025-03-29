using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using ZOUZ.Wallet.API.Endpoints;
using ZOUZ.Wallet.API.Middleware;
using ZOUZ.Wallet.Core.Interfaces.Repositories;
using ZOUZ.Wallet.Core.Interfaces.Services;
using ZOUZ.Wallet.Core.Services;
using ZOUZ.Wallet.Infrastructure.Data;
using ZOUZ.Wallet.Infrastructure.ExternalServices;
using ZOUZ.Wallet.Infrastructure.Repositories;
using ZOUZ.Wallet.Infrastructure.Services;
using BillPaymentService = ZOUZ.Wallet.Infrastructure.ExternalServices.BillPaymentService;
using KycService = ZOUZ.Wallet.Core.Services.KycService;
using NotificationService = ZOUZ.Wallet.Core.Services.NotificationService;
using PaymentGatewayService = ZOUZ.Wallet.Infrastructure.ExternalServices.PaymentGatewayService;

var builder = WebApplication.CreateBuilder(args);



// Configuration CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigins",
        builder =>
        {
            builder.WithOrigins(
                    "https://yourapp.com",
                    "https://www.yourapp.com",
                    "http://localhost:3000") // Pour le développement
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials();
        });
});

// Configuration de la base de données
builder.Services.AddDbContext<WalletDbContext>(options =>
{
    // Utiliser SQL Server
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: null);
            sqlOptions.MigrationsAssembly("ZOUZ.Wallet.Infrastructure");
        });

    // Autre option: utiliser PostgreSQL
    // options.UseNpgsql(builder.Configuration.GetConnectionString("PostgreSQLConnection"),
    //     npgsqlOptions =>
    //     {
    //         npgsqlOptions.EnableRetryOnFailure(
    //             maxRetryCount: 5,
    //             maxRetryDelay: TimeSpan.FromSeconds(30),
    //             errorCodesToAdd: null);
    //         npgsqlOptions.MigrationsAssembly("WalletAPI.Infrastructure");
    //     });
});

builder.Services.AddHttpClient();

// Enregistrement des repositories
builder.Services.AddScoped<IWalletRepository, WalletRepository>();
builder.Services.AddScoped<IOfferRepository, OfferRepository>();
builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();

// Enregistrement des services métier
builder.Services.AddScoped<IWalletService, WalletService>();
builder.Services.AddScoped<IOfferService, OfferService>();
builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IKycService, KycService>();

// Enregistrement des services d'infrastructure
builder.Services.AddScoped<IFraudDetectionService, FraudDetectionService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IPaymentGatewayService, PaymentGatewayService>();
builder.Services.AddScoped<IBillPaymentService, BillPaymentService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<ISmsService, SmsService>();

// Configuration d'AutoMapper
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

// Configuration de la validation FluentValidation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

// Configuration de l'authentification JWT
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
    };

    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = context =>
        {
            var userId = context.Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var username = context.Principal.FindFirst(ClaimTypes.Name)?.Value;

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(username))
            {
                context.Fail("Unauthorized: Invalid token");
            }

            return Task.CompletedTask;
        }
    };
});

// Configuration de l'autorisation
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("UserOnly", policy => policy.RequireRole("User"));
    options.AddPolicy("RequireAuthenticatedUser", policy => policy.RequireAuthenticatedUser());
});

// Configuration de Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "WalletAPI - Gestion de Wallet Marocain",
        Version = "v1",
        Description = "API de gestion de wallets électroniques conforme aux normes de Bank Al-Maghrib",
        Contact = new OpenApiContact
        {
            Name = "Support",
            Email = "support@walletapi.ma"
        }
    });

    // Ajout de la sécurité JWT à Swagger
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Entrez 'Bearer [token]' pour l'authentification"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    // Inclure les commentaires XML
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

// Configuration du caching
builder.Services.AddMemoryCache();
builder.Services.AddResponseCaching();

// Configuration du rate limiting
builder.Services.AddRateLimiter(limiterOptions =>
{
    // Limite globale
    limiterOptions.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.User.Identity?.Name ?? context.Request.Headers.Host.ToString(),
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 100,
                QueueLimit = 0,
                Window = TimeSpan.FromMinutes(1)
            });
    });

    // Limite spécifique pour les endpoints sensibles (transactions)
    limiterOptions.AddPolicy("TransactionEndpoints", context =>
    {
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.User.Identity?.Name ?? context.Request.Headers.Host.ToString(),
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 20,
                QueueLimit = 0,
                Window = TimeSpan.FromMinutes(1)
            });
    });

    // Réponse personnalisée en cas de limitation
    limiterOptions.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.ContentType = "application/json";

        var response = new
        {
            Success = false,
            Message = "Trop de requêtes. Veuillez réessayer plus tard."
        };

        await context.HttpContext.Response.WriteAsJsonAsync(response, token);
    };
});

// Construction de l'application
var app = builder.Build();

// Middleware de gestion des exceptions
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Configuration de l'environnement
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "WalletAPI v1");
        c.RoutePrefix = "swagger";
    });
}
else
{
    // HTTPS en production
    app.UseHsts();
}

app.UseHttpsRedirection();

// Middleware CORS
app.UseCors("AllowSpecificOrigins");

// Middlewares de sécurité et d'authentification
app.UseResponseCaching();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

// Configuration des endpoints de l'API
app.MapWalletEndpoints();
app.MapOfferEndpoints();
app.MapTransactionEndpoints();
app.MapAuthEndpoints();

// Point de terminaison pour vérifier la santé de l'API
app.MapGet("/api/health", () =>
{
    return Results.Ok(new { Status = "Healthy", Timestamp = DateTime.UtcNow });
}).WithTags("Health");

// Application des migrations au démarrage (en développement uniquement)
if (app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        try
        {
            var context = services.GetRequiredService<WalletDbContext>();
            context.Database.Migrate();
        }
        catch (Exception ex)
        {
            var logger = services.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "Une erreur est survenue lors de la migration de la base de données.");
        }
    }
}

app.Run();