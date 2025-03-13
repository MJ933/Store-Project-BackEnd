using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Npgsql;
using System.Text;
using StoreDataAccessLayer;
using StoreBusinessLayer;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);
clsDataAccessSettingsDAL.Initialize(builder.Configuration);

// Add services to the container
builder.Services.AddControllers();

// Validate and configure database connection pooling
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("Connection string 'DefaultConnection' not found in appsettings.json.");
}

builder.Services.AddNpgsqlDataSource(
    connectionString,
    npgsqlBuilder => npgsqlBuilder.EnableParameterLogging()
);

// Register DAL and BL services
builder.Services.AddScoped<clsProductsDAL>();
builder.Services.AddScoped<clsProductsBL>();

builder.Services.AddScoped<clsImagesDAL>();
builder.Services.AddScoped<clsImagesBL>();
builder.Services.AddScoped<ICategoriesRepository, CategoriesRepository>();
builder.Services.AddScoped<ICategoriesService, CategoriesService>();
builder.Services.AddScoped<clsCustomersDAL>();
builder.Services.AddScoped<clsCustomersBL>();
builder.Services.AddScoped<clsEmployeesDAL>();
builder.Services.AddScoped<clsEmployeesBL>();
builder.Services.AddScoped<IOrdersRepository, OrdersRepository>();
builder.Services.AddScoped<IOrdersService, OrdersService>();
builder.Services.AddScoped<clsOrderItemsDAL>();
builder.Services.AddScoped<clsOrderItemsBL>();


// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalDevelopment", policy =>
        policy.WithOrigins("http://localhost:5173") // Your React dev server
              .AllowAnyMethod()
              .AllowAnyHeader());

    options.AddPolicy("AllowNetlifyProduction", policy =>
        policy.WithOrigins("https://alrafidainstore.netlify.app") // Your Netlify domain
              .AllowAnyMethod()
              .AllowAnyHeader());
});

// Configure JWT Authentication
var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.ASCII.GetBytes(jwtSettings["Key"]!);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

// Configure Authorization Policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("admin", policy => policy.RequireRole("admin"));
    options.AddPolicy("sales", policy => policy.RequireRole("sales"));
    options.AddPolicy("marketing", policy => policy.RequireRole("marketing"));
});

// Configure Swagger
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Store API", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
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
});

// Register Token Service
builder.Services.AddSingleton<TokenService>(sp =>
    new TokenService(builder.Configuration)
);

var app = builder.Build();

// Configure middleware pipeline
if (app.Environment.IsDevelopment())
{
    app.UseCors("AllowLocalDevelopment"); // Use the development policy
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Store API v1");
        options.ConfigObject.AdditionalItems["docExpansion"] = "none";
    });
}
else
{
    app.UseCors("AllowNetlifyProduction"); // Use the production policy
}

app.UseMiddleware<PrerenderMiddleware>();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

// Token Service implementation
public class TokenService
{
    private readonly IConfiguration _configuration;

    public TokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateToken(string userId, string? email, string? phoneNumber, string? role = null)
    {
        return JwtHelper.GenerateToken(userId, email, phoneNumber, _configuration, role);
    }
}