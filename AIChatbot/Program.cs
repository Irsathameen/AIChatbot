
using AIChatbot.Utility;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddCors(options =>
{
    options.AddPolicy("LocalAngularDev", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddHttpClient<OllamaService>();
builder.Services.AddHttpClient<OllamaEmbeddingService>();
builder.Services.AddScoped<DocumentChunkRepository>();
builder.Services.AddScoped<RagService>();

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
