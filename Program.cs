using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using API_Ecommerce.Commands.Create;
using API_Ecommerce.Commands.Suspend;
using API_Ecommerce.Commands.Update;
using API_Ecommerce.Commands.Delete;
using API_Ecommerce.Data;
using API_Ecommerce.Queries;
using API_Ecommerce.Services;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Scalar.AspNetCore;
using API_Ecommerce.Models;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);

// Default to Development when running locally without explicit environment set
if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")))
{
    builder.Environment.EnvironmentName = Environments.Development;
}

// ==========================================
// 1. Database Context
// ==========================================
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ==========================================
// 2. MediatR Registration (Scans Assembly for Handlers)
// ==========================================
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

// ==========================================
// 3. Register Commands & Queries (DI Services)
// ==========================================
// Auth Commands & Queries
builder.Services.AddScoped<CreateAuthCommand>();
builder.Services.AddScoped<UpdateAuthCommand>();
builder.Services.AddScoped<SuspendAuthCommand>();
builder.Services.AddScoped<AuthQueries>();

// Categories & Product Queries
builder.Services.AddScoped<CategoriesQueries>();
builder.Services.AddScoped<ProductQueries>();

//Order
builder.Services.AddScoped<CreateOrderCommand>();
builder.Services.AddScoped<OrderQueries>();
builder.Services.AddScoped<CreateOrderCommand>();
builder.Services.AddScoped<UpdateOrderStatusCommandHandler>();

// Register Address Queries & Commands
builder.Services.AddScoped<AddressQueries>();
builder.Services.AddScoped<CreateAddressCommand>();
builder.Services.AddScoped<UpdateAddressCommand>();
builder.Services.AddScoped<DeleteAddressCommand>();

// Payment
builder.Services.AddScoped<PaymentQueries>();
builder.Services.AddScoped<CreatePaymentCommandHandler>();
builder.Services.AddScoped<VerifyPaymentCommandHandler>();

// ProductVariants 
builder.Services.AddScoped<ProductVariantQueries>();

//OrderItem
builder.Services.AddScoped<OrderItemQueries>();

// Favorite
builder.Services.AddScoped<FavoriteQueries>();

// GeneratePayment
builder.Services.AddScoped<GenerateOrderQrQuery>();

// Banner
builder.Services.AddScoped<BannerQueries>();

// Review 
builder.Services.AddScoped<ReviewQueries>();

// telegram notification service
builder.Services.AddHttpClient<ITelegramNotificationService, TelegramNotificationService>();

// Token & Infrastructure Services
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.Configure<BakongSettings>(builder.Configuration.GetSection("Bakong"));
builder.Services.AddHttpClient<IBakongService, BakongService>();
builder.Services.AddScoped<ISellerBakongService, SellerBakongService>();

// ==========================================
// 4. JWT Authentication Setup
// ==========================================
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["Secret"]
    ?? throw new InvalidOperationException("JWT Secret key is missing in appsettings.json.");

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
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),

        RoleClaimType = ClaimTypes.Role,
        NameClaimType = ClaimTypes.Name
    };
});

// 5. Antiforgery (CSRF Defense) & Rate Limiting Setup
var keysDirectory = Path.Combine(builder.Environment.ContentRootPath, "App_Data", "DataProtection-Keys");
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(keysDirectory))
    .SetApplicationName("API_Ecommerce");

builder.Services.AddAntiforgery(options =>
{
    options.Cookie.Name = ".AspNetCore.Antiforgery";
    options.Cookie.HttpOnly = false;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.HeaderName = "X-XSRF-TOKEN";
});

// ==========================================
// 6. Controllers & JSON Formatting
// ==========================================
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        // Convert Enums to readable strings ("Draft", "Pending", "Approved", "Suspended", etc.)
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    })
    .AddMvcOptions(options =>
    {
        options.ModelMetadataDetailsProviders.Add(
            new Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.SystemTextJsonValidationMetadataProvider());
    });
// ==========================================
// 6. OpenAPI / Scalar setup with Global Bearer Auth
// ==========================================
builder.Services.AddOpenApi("v1", options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();

        // 1. Define Bearer Security Scheme
        var bearerScheme = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Enter your JWT Bearer token."
        };

        // 2. Register Scheme in Components
        document.Components.SecuritySchemes["Bearer"] = bearerScheme;

        // 3. Apply Scheme Globally using OpenApiSecuritySchemeReference
        document.Security ??= new List<OpenApiSecurityRequirement>();
        document.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", document)] = new List<string>()
        });

        return Task.CompletedTask;
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("LocalDevPolicy", policy =>
    {
        policy.WithOrigins("http://localhost:4200", "http://127.0.0.1:4200")
              .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS")
              .WithHeaders("Content-Type", "Authorization", "X-Requested-With", "X-XSRF-TOKEN", "Accept", "Origin")
              .AllowCredentials();
    });
});

// ==========================================
// 8. Rate Limiting Setup
// ==========================================

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.ContentType = "application/problem+json";
        context.HttpContext.Response.Headers["Retry-After"] = "60";

        var problemDetails = new Microsoft.AspNetCore.Mvc.ProblemDetails
        {
            Status = StatusCodes.Status429TooManyRequests,
            Title = "Too Many Requests",
            Detail = "Rate limit exceeded. Please wait a moment before trying again.",
            Instance = context.HttpContext.Request.Path
        };

        await context.HttpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken: cancellationToken);
    };

    options.AddPolicy("AuthRateLimit", httpContext =>
    {
        if (HttpMethods.IsOptions(httpContext.Request.Method))
        {
            return RateLimitPartition.GetNoLimiter("options");
        }
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            });
    });

    options.AddPolicy("CheckoutRateLimit", httpContext =>
    {
        if (HttpMethods.IsOptions(httpContext.Request.Method))
        {
            return RateLimitPartition.GetNoLimiter("options");
        }
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            });
    });
});

// ==========================================
// 9. Build App & Middleware Pipeline
// ==========================================
var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        await DbInitializer.SeedAdminAsync(context);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the Admin account.");
    }
}

// OpenAPI / Scalar API Documentation
app.MapOpenApi();
app.MapScalarApiReference(options =>
{
    options.WithTitle("E-Commerce API")
           .WithTheme(ScalarTheme.Moon)
           .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
});
app.MapGet("/scalar", () => Results.Redirect("/scalar/v1"));
app.MapGet("/", () => Results.Redirect("/scalar/v1"));

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// ==========================================
// 10. Middleware Pipeline Order
// ==========================================

// Global RFC 7807 Exception Handler (sanitized error masking)
app.UseExceptionHandler(exceptionHandlerApp =>
{
    exceptionHandlerApp.Run(async context =>
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/problem+json";

        var exceptionHandlerPathFeature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerPathFeature>();
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        if (exceptionHandlerPathFeature?.Error != null)
        {
            logger.LogError(exceptionHandlerPathFeature.Error, "Unhandled exception at {Path}", context.Request.Path);
        }

        var problemDetails = new Microsoft.AspNetCore.Mvc.ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "An error occurred while processing your request.",
            Detail = "An unexpected error occurred. Please try again later.",
            Instance = context.Request.Path,
            Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1"
        };
        problemDetails.Extensions["traceId"] = context.TraceIdentifier;

        await context.Response.WriteAsJsonAsync(problemDetails);
    });
});

// 1. CORS MUST be placed before Static Files, Security Headers, Authentication, Rate Limiting, and Authorization
app.UseCors("LocalDevPolicy");

// 2. Configure Static File Options with CORS response headers
if (!string.IsNullOrEmpty(app.Environment.WebRootPath) && !Directory.Exists(app.Environment.WebRootPath))
{
    Directory.CreateDirectory(app.Environment.WebRootPath);
}

var staticFileOptions = new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        ctx.Context.Response.Headers.AccessControlAllowOrigin = "*";
        ctx.Context.Response.Headers.AccessControlAllowMethods = "GET, HEAD, OPTIONS";
        ctx.Context.Response.Headers.AccessControlAllowHeaders = "*";
    }
};

app.UseStaticFiles(staticFileOptions);

// 3. Configure external /uploads physical directory mapping if present
var uploadsPath = Path.Combine(builder.Environment.ContentRootPath, "uploads");
if (Directory.Exists(uploadsPath))
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(uploadsPath),
        RequestPath = "/uploads",
        OnPrepareResponse = ctx =>
        {
            ctx.Context.Response.Headers.AccessControlAllowOrigin = "*";
            ctx.Context.Response.Headers.AccessControlAllowMethods = "GET, HEAD, OPTIONS";
            ctx.Context.Response.Headers.AccessControlAllowHeaders = "*";
        }
    });
}

// Security Headers (XSS & Frame Defense)
app.Use(async (context, next) =>
{
    // Relax CSP for Scalar API documentation & OpenAPI spec so Scalar UI scripts/styles render cleanly
    if (context.Request.Path.StartsWithSegments("/scalar") || context.Request.Path.StartsWithSegments("/openapi"))
    {
        context.Response.Headers["Content-Security-Policy"] = "default-src 'self' 'unsafe-inline' 'unsafe-eval' https: http: data: blob:;";
    }
    else
    {
        context.Response.Headers["Content-Security-Policy"] = "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'; img-src 'self' data: https:; connect-src 'self' https: http:;";
    }
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    await next();
});

// Rate Limiting Middleware
app.UseRateLimiter();

// CSRF Token Cookie Middleware (issue/refresh XSRF-TOKEN on GET requests)
app.Use(async (context, next) =>
{
    try
    {
        var antiforgery = context.RequestServices.GetRequiredService<IAntiforgery>();
        var tokens = antiforgery.GetAndStoreTokens(context);
        if (tokens.RequestToken != null)
        {
            context.Response.Cookies.Append("XSRF-TOKEN", tokens.RequestToken, new CookieOptions
            {
                HttpOnly = false,
                SameSite = SameSiteMode.Lax,
                Secure = context.Request.IsHttps,
                Path = "/"
            });
        }
    }
    catch (Exception)
    {
        // Stale cookie from a prior dev session; remove it so next request gets clean tokens
        context.Response.Cookies.Delete(".AspNetCore.Antiforgery");
    }
    await next();
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapGet("/api/csrf-token", (IAntiforgery antiforgery, HttpContext context) =>
{
    var tokens = antiforgery.GetAndStoreTokens(context);
    return Results.Ok(new { token = tokens.RequestToken, headerName = "X-XSRF-TOKEN" });
});

app.Urls.Add("http://localhost:5000");

app.Run();
