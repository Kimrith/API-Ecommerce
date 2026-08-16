using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using API_Ecommerce.Commands.Create;
using API_Ecommerce.Commands.Suspend;
using API_Ecommerce.Commands.Update;
using API_Ecommerce.Commands.Delete;
using API_Ecommerce.Data;
using API_Ecommerce.Queries;
using API_Ecommerce.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Scalar.AspNetCore;
using API_Ecommerce.Models;

var builder = WebApplication.CreateBuilder(args);

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

// ==========================================
// 5. Controllers & JSON Formatting
// ==========================================
builder.Services.AddControllers()
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

// ==========================================
// 7. CORS Configuration (Allow All)
// ==========================================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular",
        policy => policy.WithOrigins("http://localhost:4200", 
                        "http://127.0.0.1:4200",
                        "http://localhost:54902")
                        .AllowAnyHeader()
                        .AllowAnyMethod());
});

// ==========================================
// 8. Build App & Middleware Pipeline
// ==========================================
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("E-Commerce API")
               .WithTheme(ScalarTheme.Moon)
               .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
    });
}

app.UseHttpsRedirection();

// ==========================================
// 9. Middleware Pipeline Order
// ==========================================
app.UseStaticFiles(); // Serves uploaded product, category & user images from wwwr
// 👈 CORS MUST be placed before Authentication and Authorization
app.UseCors("AllowAngular");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();