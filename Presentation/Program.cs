using Microsoft.OpenApi.Models;
using Presentation;
using Presentation.Interfaces;
using Presentation.Models;
using Presentation.Services;
using Swashbuckle.AspNetCore.Filters;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddGrpc();

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v.1.0",
        Title = "Profile Service API Documentation",
        Description = "Official doucumentation for Profile Service Provider API."
    });

    options.EnableAnnotations();
    options.ExampleFilters();

    var apiScheme = new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Name = "X-API-KEY",
        Description = "API key header Required. Example: X-API-KEY: your_api_key_here",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "ApiKeyScheme",
        Reference = new OpenApiReference
        {
            Id = "ApiKey",
            Type = ReferenceType.SecurityScheme,
        }
    };

    options.AddSecurityDefinition("ApiKey", apiScheme);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { apiScheme, new List<string>() }
    });
});

builder.Services.AddSwaggerExamplesFromAssemblyOf<Program>();

builder.Services.AddCors(option => { option.AddPolicy("AllowAll", policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()); });

builder.Services.Configure<ApiSettings>(builder.Configuration.GetSection("ApiSettings"));

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddGrpcClient<AccountGrpcService.AccountGrpcServiceClient>(option =>
{
    option.Address = new Uri(builder.Configuration["Providers:AccountServiceProvider"]!);
});

builder.Services.AddSingleton<IAuthServiceBusHandler, AuthServiceBusHandler>();

var app = builder.Build();
app.MapOpenApi();
app.UseSwagger();
app.UseSwaggerUI(option =>
{
    option.SwaggerEndpoint("/swagger/v1/swagger.json", "Ventixe AuthServiceProvider API");
    option.RoutePrefix = string.Empty;
});

app.MapGrpcService<AuthService>();
app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

app.UseHsts();
app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
