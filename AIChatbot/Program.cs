using AIChatbot.Utility;
using Microsoft.Extensions.AI;

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

builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Enable CORS for the configured policy
app.UseCors("LocalAngularDev");

app.UseAuthorization();

app.MapControllers();

app.Run();
