using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using ICCMS_API.Authentication;
using ICCMS_API.Services;

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
builder.Services.AddSwaggerGen();

// Add Firebase services
builder.Services.AddScoped<IFirebaseService, FirebaseService>();
builder.Services.AddScoped<IAuthService, AuthService>();

// Add Supabase service
builder.Services.AddScoped<ISupabaseService, SupabaseService>();

// Add workflow services
builder.Services.AddScoped<IQuoteWorkflowService, QuoteWorkflowService>();
builder.Services.AddScoped<IInvoiceWorkflowService, InvoiceWorkflowService>();

// Add AI processing and material database services
builder.Services.AddScoped<IAiProcessingService, AiProcessingService>();
builder.Services.AddScoped<IMaterialDatabaseService, MaterialDatabaseService>();

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
                    "https://localhost:7271",
                    "http://localhost:5148",
                    "http://localhost:5031"
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
