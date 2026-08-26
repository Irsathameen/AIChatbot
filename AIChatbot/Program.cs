using AIChatbot.Services;
using AIChatbot.Utility;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
// Add services to the container.
builder.Services.AddControllers();

// Configure CORS for local Angular dev server
builder.Services.AddCors(options =>
{
    options.AddPolicy("LocalAngularDev", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

//Add OllamaService
builder.Services.AddHttpClient<OllamaService>();

// Register Postgres DbContext for RAG storage (pgvector must be enabled in DB)
builder.Services.AddDbContext<ChatbotDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"),
                      o => o.UseVector());
});

// Register embedding and RAG services
builder.Services.AddSingleton<IEmbeddingService, SimpleEmbeddingService>();
builder.Services.AddScoped<RagService>();
builder.Services.AddScoped<PdfIngestor>();
// 1. Register the native .NET 10 OpenAPI document generator
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

app.UseAuthorization();

app.MapControllers();

app.Run();
