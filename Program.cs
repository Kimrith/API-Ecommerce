using API_Ecommerce.Commands.Create;
using API_Ecommerce.Commands.Update;
using API_Ecommerce.Data;
using API_Ecommerce.Queries;
using API_Ecommerce.Services;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// 1. Database Context
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. Register Commands & Queries
builder.Services.AddScoped<CreateAuthCommand>();
builder.Services.AddScoped<UpdateAuthCommand>();
builder.Services.AddScoped<AuthQueries>();

// Token Service
builder.Services.AddScoped<ITokenService, TokenService>();

// 3. Controllers & OpenAPI (Scalar backend engine)
builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

// 4. Configure HTTP request pipeline & Scalar API UI
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
app.UseStaticFiles(); // Serves uploaded profile images from wwwroot
app.UseAuthorization();

app.MapControllers();

app.Run();