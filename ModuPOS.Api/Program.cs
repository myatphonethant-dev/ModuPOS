using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using ModuPOS.Auth;
using ModuPOS.Database;
using ModuPOS.Delivery;
using ModuPOS.Inventory;
using ModuPOS.Organization;
using ModuPOS.Reporting;
using ModuPOS.Sale;
using ModuPOS.Loyalty;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add Controllers & OpenAPI
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Register Feature Modules
builder.Services.AddModuPOSDatabase(builder.Configuration);
builder.Services.AddAuthModule(builder.Configuration);
builder.Services.AddInventoryModule();
builder.Services.AddOrganizationModule();
builder.Services.AddDeliveryModule();
builder.Services.AddReportingModule();
builder.Services.AddSaleModule();
builder.Services.AddLoyaltyModule();

// JWT Authentication
var jwtSecret = builder.Configuration["Jwt:Secret"] ?? "ModuPOS_Super_Secret_Key_For_Jwt_Signing_Must_Be_Long_Enough_2026";
var issuer = builder.Configuration["Jwt:Issuer"] ?? "ModuPOS";
var audience = builder.Configuration["Jwt:Audience"] ?? "ModuPOS.Api";

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
        ValidIssuer = issuer,
        ValidAudience = audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.MapScalarApiReference();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program;