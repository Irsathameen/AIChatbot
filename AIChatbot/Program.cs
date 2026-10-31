
using AIChatbot.Utility;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("JWT signing key 'Jwt:Key' is not configured.");
var jwtKeyBytes = Encoding.UTF8.GetBytes(jwtKey);
if (jwtKeyBytes.Length < 32)
{
    throw new InvalidOperationException("JWT signing key 'Jwt:Key' must be at least 32 bytes long.");
}

var jwtIssuer = builder.Configuration["Jwt:Issuer"]
    ?? throw new InvalidOperationException("JWT issuer 'Jwt:Issuer' is not configured.");
var jwtAudience = builder.Configuration["Jwt:Audience"]
    ?? throw new InvalidOperationException("JWT audience 'Jwt:Audience' is not configured.");
var signingKey = new SymmetricSecurityKey(jwtKeyBytes);

builder.Services.AddCors(options =>
{
    options.AddPolicy("LocalAngularDev", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = signingKey,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddHttpClient<OllamaService>();
builder.Services.AddHttpClient<OllamaEmbeddingService>();
builder.Services.AddScoped<DocumentChunkRepository>();
builder.Services.AddScoped<RagService>();
builder.Services.AddScoped<AuthRepository>();
builder.Services.AddScoped<Microsoft.AspNetCore.Identity.PasswordHasher<AuthUser>>();
builder.Services.AddSingleton(new JwtTokenService(signingKey, jwtIssuer, jwtAudience));

builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // Show detailed developer exceptions in development
    app.UseDeveloperExceptionPage();

    // Generates the native /openapi/v1.json endpoint
    app.MapOpenApi();

    // Attach Swagger UI and point it to the .NET 10 OpenAPI JSON endpoint
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "API v1");
    });
}

app.UseHttpsRedirection();

// Enable CORS for the configured policy
app.UseCors("LocalAngularDev");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
