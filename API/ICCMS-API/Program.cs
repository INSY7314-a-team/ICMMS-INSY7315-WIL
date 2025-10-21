using System.IO;
using System.Linq;
using System.Reflection;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using ICCMS_API.Authentication;
using ICCMS_API.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

// Initialize Firebase Admin SDK BEFORE creating the builder
try
{
    // Get configuration from environment or user secrets
    var configuration = new ConfigurationBuilder()
        .AddEnvironmentVariables()
        .AddUserSecrets<Program>()
        .Build();

    var projectId = configuration["Firebase:project_id"] ?? "icmms-ecba6";
    var credentialsPath = configuration["Firebase:CredentialsPath"] ?? "firebase-credentials.json";

    if (File.Exists(credentialsPath))
    {
        var credential = GoogleCredential.FromFile(credentialsPath);
        FirebaseApp.Create(new AppOptions { Credential = credential, ProjectId = projectId });
        Console.WriteLine("Firebase Admin SDK initialized successfully");
    }
    else
    {
        Console.WriteLine($"Firebase credentials file not found at: {credentialsPath}");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Error initializing Firebase: {ex.Message}");
}

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "ICCMS API", Version = "v1" });

    //c.OperationFilter<FileUploadOperationFilter>();
});

// Add Firebase services
builder.Services.AddScoped<IFirebaseService, FirebaseService>();
builder.Services.AddScoped<IAuthService, AuthService>();

// Add notification service
builder.Services.AddScoped<INotificationService, NotificationService>();

// Add Supabase service
builder.Services.AddScoped<ISupabaseService, SupabaseService>();

// Add message validation service
builder.Services.AddScoped<IMessageValidationService, MessageValidationService>();

// Add workflow message service
builder.Services.AddScoped<IWorkflowMessageService, WorkflowMessageService>();

// Add workflow services
builder.Services.AddScoped<IQuoteWorkflowService, QuoteWorkflowService>();
builder.Services.AddScoped<IInvoiceWorkflowService, InvoiceWorkflowService>();

// Add HttpClient for external API calls
builder.Services.AddHttpClient();

// Add AI processing and material database services
builder.Services.AddScoped<IAiProcessingService, AiProcessingService>();
builder.Services.AddScoped<IMaterialDatabaseService, MaterialDatabaseService>();
builder.Services.AddScoped<SupabaseBlueprintService>();

// Add Authentication
builder
    .Services.AddAuthentication("Bearer")
    .AddScheme<FirebaseAuthSchemeOptions, FirebaseAuthHandler>("Bearer", options => { });

builder.Services.AddAuthorization();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy(
        "AllowSpecificOrigins",
        policy =>
        {
            policy
                .WithOrigins(
                    "https://localhost:7271", // check this
                    "http://localhost:5148", // web server
                    "http://localhost:5031" // check this
                )
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials();
        }
    );
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowSpecificOrigins");

// Add authentication and authorization middleware
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

public class FileUploadOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var fileParams = context
            .MethodInfo.GetParameters()
            .Where(p => p.ParameterType == typeof(IFormFile))
            .ToList();

        if (fileParams.Any())
        {
            operation.RequestBody = new OpenApiRequestBody
            {
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    ["multipart/form-data"] = new OpenApiMediaType
                    {
                        Schema = new OpenApiSchema
                        {
                            Type = "object",
                            Properties = new Dictionary<string, OpenApiSchema>
                            {
                                ["file"] = new OpenApiSchema { Type = "string", Format = "binary" },
                                ["projectId"] = new OpenApiSchema { Type = "string" },
                                ["description"] = new OpenApiSchema { Type = "string" },
                            },
                        },
                    },
                },
            };
        }
    }
}
