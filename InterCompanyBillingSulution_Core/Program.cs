using InterCompanyBillingSulution.DataAcess.Data;
using InterCompanyBillingSulution.Models.DTO;
using InterCompanyBillingSulution_Core;
using InterCompanyBillingSulution_Core.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using OfficeOpenXml;
using System.Security.Cryptography;
using System.Text;


var builder = WebApplication.CreateBuilder(args);

ExcelPackage.License.SetNonCommercialPersonal("InterCompany Billing");

// ================== CORS ==================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod());
});

// ================== Controllers ==================
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// ================== Swagger ==================
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "InterCompanyBilling API", Version = "v1" });

    // ✅ Add JWT Authentication to Swagger
    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter: Bearer {your token}"
    };
    c.AddSecurityDefinition("Bearer", securityScheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// ================== DB Context ==================
builder.Services.AddDbContext<InterCompanyBillingDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("ConnectionStr")));

// ================== Identity ==================
builder.Services.AddIdentityCore<IdentityUser>()
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<InterCompanyBillingDbContext>();

// ================== Session ==================
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.None;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

// ================== Services ==================
builder.Services.AddTransient<IEmailService, EmailService>();
builder.Services.AddScoped<AuditService>();
builder.Services.AddScoped<GenerateTemplate>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<PasswordEncryptionService>();

// ================== JWT Authentication ==================
var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]);
var encryptkey = Convert.ToBase64String(
    RandomNumberGenerator.GetBytes(32)
);


builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false; // only for localhost
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
});

builder.Services.AddAuthorization();
builder.Services.AddHttpClient<PythonAIService>(c => c.Timeout = TimeSpan.FromMinutes(5));
builder.Services.AddScoped<DataQueryService>();


var app = builder.Build();

// ================== Middleware order ==================
app.UseCors("AllowAll");

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(Directory.GetCurrentDirectory(), "UploadedFiles")),
    RequestPath = "/UploadedFiles",

    OnPrepareResponse = ctx =>
    {
        ctx.Context.Response.Headers.Append(
            "Access-Control-Allow-Origin", "*");

        ctx.Context.Response.Headers.Append(
            "Access-Control-Allow-Headers", "*");

        ctx.Context.Response.Headers.Append(
            "Access-Control-Allow-Methods", "GET, OPTIONS");
    }
});

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(Directory.GetCurrentDirectory(), "PaymentUploads")),
    RequestPath = "/PaymentUploads",

    OnPrepareResponse = ctx =>
    {
        ctx.Context.Response.Headers.Append("Access-Control-Allow-Origin", "*");
        ctx.Context.Response.Headers.Append("Access-Control-Allow-Headers", "*");
        ctx.Context.Response.Headers.Append("Access-Control-Allow-Methods", "GET, OPTIONS");
    }
});

app.UseSession();

app.UseRouting();

app.UseAuthentication(); // ✅ must be before Authorization
app.UseAuthorization();

app.MapControllers();

app.Run();
