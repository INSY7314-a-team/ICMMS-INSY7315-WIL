I'll help you create a comprehensive implementation guide for setting up your API and Web applications to integrate with Firebase Firestore and Authentication based on your ERD. Let me first examine your current project structure and then provide a detailed guide.

Read file: API/ICCMS-API/Program.cs
Read file: API/ICCMS-API/ICCMS-API.csproj
Read file: Web/ICCMS-Web/Program.cs
Read file: Web/ICCMS-Web/ICCMS-Web.csproj
Now I'll create a comprehensive implementation guide for setting up your API and Web applications with Firebase integration.

# Implementation Guide: Setting Up ICCMS with Firebase Integration

## Overview

This guide will help you set up your ICCMS (Integrated Construction and Maintenance Management System) API and Web applications to integrate with Firebase Firestore NoSQL Database and Firebase Authentication, based on your ERD.

## Prerequisites

- .NET 8.0 SDK
- Visual Studio 2022 or VS Code
- Firebase Project (create at [Firebase Console](https://console.firebase.google.com/))
- Node.js (for Firebase CLI)

## Part 1: Firebase Project Setup

### 1.1 Create Firebase Project

1. Go to [Firebase Console](https://console.firebase.google.com/)
2. Click "Add project"
3. Name your project: `ICMMS-Project`
4. Enable Google Analytics (optional)
5. Choose analytics account or create new one
6. Click "Create project"

### 1.2 Enable Authentication

1. In Firebase Console, go to **Authentication** → **Sign-in method**
2. Enable the following providers:
   - **Email/Password**
   - **Google** (for OAuth)
   - **Phone** (optional)
3. Configure each provider as needed

### 1.3 Create Firestore Database

1. Go to **Firestore Database** → **Create Database**
2. Choose **Start in test mode** (for development)
3. Select a location close to your users
4. Click **Done**

### 1.4 Get Firebase Configuration

1. Go to **Project Settings** (gear icon)
2. Scroll down to **Your apps** section
3. Click **Add app** → **Web**
4. Register app with name: `ICMMS-Web`
5. Copy the Firebase config object (you'll need this later)

## Part 2: API Setup

### 2.1 Install Required NuGet Packages

Update your `API/ICCMS-API/ICCMS-API.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <RootNamespace>ICCMS_API</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Swashbuckle.AspNetCore" Version="6.6.2" />
    <PackageReference Include="FirebaseAdmin" Version="2.4.0" />
    <PackageReference Include="Google.Cloud.Firestore" Version="3.7.0" />
    <PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="8.0.0" />
    <PackageReference Include="Microsoft.AspNetCore.Cors" Version="2.2.0" />
    <PackageReference Include="AutoMapper" Version="12.0.1" />
    <PackageReference Include="AutoMapper.Extensions.Microsoft.DependencyInjection" Version="12.0.1" />
    <PackageReference Include="FluentValidation.AspNetCore" Version="11.3.0" />
  </ItemGroup>

</Project>
```

### 2.2 Create Firebase Configuration

Create `API/ICCMS-API/appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "Firebase": {
    "ProjectId": "your-firebase-project-id",
    "PrivateKeyId": "your-private-key-id",
    "PrivateKey": "your-private-key",
    "ClientEmail": "your-client-email",
    "ClientId": "your-client-id",
    "AuthUri": "https://accounts.google.com/o/oauth2/auth",
    "TokenUri": "https://oauth2.googleapis.com/token",
    "AuthProviderX509CertUrl": "https://www.googleapis.com/oauth2/v1/certs",
    "ClientX509CertUrl": "your-cert-url"
  },
  "Jwt": {
    "Issuer": "your-firebase-project-id",
    "Audience": "your-firebase-project-id"
  },
  "Cors": {
    "AllowedOrigins": ["https://localhost:7001", "https://localhost:7002"]
  }
}
```

### 2.3 Create Models Based on ERD

Create `API/ICCMS-API/Models/` directory and add the following models:

**User.cs:**

```csharp
using System.ComponentModel.DataAnnotations;

namespace ICCMS_API.Models
{
    public class User
    {
        public string UserId { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
```

**Project.cs:**

```csharp
namespace ICCMS_API.Models
{
    public class Project
    {
        public string ProjectId { get; set; } = string.Empty;
        public string ProjectManagerId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal BudgetPlanned { get; set; }
        public decimal BudgetActual { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDatePlanned { get; set; }
        public DateTime? EndDateActual { get; set; }
    }
}
```

**MaintenanceRequest.cs:**

```csharp
namespace ICCMS_API.Models
{
    public class MaintenanceRequest
    {
        public string MaintenanceRequestId { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;
        public string ProjectId { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string MediaUrl { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
    }
}
```

**Quotation.cs:**

```csharp
namespace ICCMS_API.Models
{
    public class Quotation
    {
        public string QuotationId { get; set; } = string.Empty;
        public string ProjectId { get; set; } = string.Empty;
        public string MaintenanceRequestId { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;
        public string AdminApproverUserId { get; set; } = string.Empty;
        public decimal Total { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? SentAt { get; set; }
    }
}
```

### 2.4 Create Firebase Service

Create `API/ICCMS-API/Services/FirebaseService.cs`:

```csharp
using Google.Cloud.Firestore;
using ICCMS_API.Models;
using Microsoft.Extensions.Configuration;

namespace ICCMS_API.Services
{
    public interface IFirebaseService
    {
        Task<T> GetDocumentAsync<T>(string collection, string documentId) where T : class;
        Task<List<T>> GetCollectionAsync<T>(string collection) where T : class;
        Task<string> AddDocumentAsync<T>(string collection, T document) where T : class;
        Task UpdateDocumentAsync<T>(string collection, string documentId, T document) where T : class;
        Task DeleteDocumentAsync(string collection, string documentId);
    }

    public class FirebaseService : IFirebaseService
    {
        private readonly FirestoreDb _firestoreDb;

        public FirebaseService(IConfiguration configuration)
        {
            var projectId = configuration["Firebase:ProjectId"];
            _firestoreDb = FirestoreDb.Create(projectId);
        }

        public async Task<T> GetDocumentAsync<T>(string collection, string documentId) where T : class
        {
            var document = await _firestoreDb.Collection(collection).Document(documentId).GetSnapshotAsync();
            return document.ConvertTo<T>();
        }

        public async Task<List<T>> GetCollectionAsync<T>(string collection) where T : class
        {
            var snapshot = await _firestoreDb.Collection(collection).GetSnapshotAsync();
            return snapshot.Documents.Select(doc => doc.ConvertTo<T>()).ToList();
        }

        public async Task<string> AddDocumentAsync<T>(string collection, T document) where T : class
        {
            var docRef = await _firestoreDb.Collection(collection).AddAsync(document);
            return docRef.Id;
        }

        public async Task UpdateDocumentAsync<T>(string collection, string documentId, T document) where T : class
        {
            await _firestoreDb.Collection(collection).Document(documentId).SetAsync(document);
        }

        public async Task DeleteDocumentAsync(string collection, string documentId)
        {
            await _firestoreDb.Collection(collection).Document(documentId).DeleteAsync();
        }
    }
}
```

### 2.5 Create Authentication Service

Create `API/ICCMS-API/Services/AuthService.cs`:

```csharp
using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Configuration;

namespace ICCMS_API.Services
{
    public interface IAuthService
    {
        Task<FirebaseToken> VerifyTokenAsync(string idToken);
        Task<UserRecord> GetUserAsync(string uid);
        Task<string> CreateUserAsync(string email, string password, string displayName);
    }

    public class AuthService : IAuthService
    {
        private readonly FirebaseAuth _auth;

        public AuthService(IConfiguration configuration)
        {
            if (FirebaseApp.DefaultInstance == null)
            {
                FirebaseApp.Create(new AppOptions
                {
                    Credential = GoogleCredential.FromFile("path-to-your-service-account-key.json")
                });
            }
            _auth = FirebaseAuth.DefaultInstance;
        }

        public async Task<FirebaseToken> VerifyTokenAsync(string idToken)
        {
            return await _auth.VerifyIdTokenAsync(idToken);
        }

        public async Task<UserRecord> GetUserAsync(string uid)
        {
            return await _auth.GetUserAsync(uid);
        }

        public async Task<string> CreateUserAsync(string email, string password, string displayName)
        {
            var userArgs = new UserRecordArgs
            {
                Email = email,
                Password = password,
                DisplayName = displayName
            };
            var userRecord = await _auth.CreateUserAsync(userArgs);
            return userRecord.Uid;
        }
    }
}
```

### 2.6 Create Controllers

Create `API/ICCMS-API/Controllers/UsersController.cs`:

```csharp
using Microsoft.AspNetCore.Mvc;
using ICCMS_API.Models;
using ICCMS_API.Services;

namespace ICCMS_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IFirebaseService _firebaseService;
        private readonly IAuthService _authService;

        public UsersController(IFirebaseService firebaseService, IAuthService authService)
        {
            _firebaseService = firebaseService;
            _authService = authService;
        }

        [HttpGet]
        public async Task<ActionResult<List<User>>> GetUsers()
        {
            try
            {
                var users = await _firebaseService.GetCollectionAsync<User>("users");
                return Ok(users);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<User>> GetUser(string id)
        {
            try
            {
                var user = await _firebaseService.GetDocumentAsync<User>("users", id);
                if (user == null)
                    return NotFound();
                return Ok(user);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost]
        public async Task<ActionResult<string>> CreateUser([FromBody] User user)
        {
            try
            {
                user.CreatedAt = DateTime.UtcNow;
                var userId = await _firebaseService.AddDocumentAsync("users", user);
                return Ok(userId);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(string id, [FromBody] User user)
        {
            try
            {
                await _firebaseService.UpdateDocumentAsync("users", id, user);
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            try
            {
                await _firebaseService.DeleteDocumentAsync("users", id);
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}
```

### 2.7 Update Program.cs

Update `API/ICCMS-API/Program.cs`:

```csharp
using ICCMS_API.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add Firebase services
builder.Services.AddScoped<IFirebaseService, FirebaseService>();
builder.Services.AddScoped<IAuthService, AuthService>();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigins",
        builder =>
        {
            builder.WithOrigins("https://localhost:7001", "https://localhost:7002")
                   .AllowAnyMethod()
                   .AllowAnyHeader()
                   .AllowCredentials();
        });
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
app.UseAuthorization();
app.MapControllers();

app.Run();
```

## Part 3: Web Application Setup

### 3.1 Install Required NuGet Packages

Update `Web/ICCMS-Web/ICCMS-Web.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <RootNamespace>ICCMS_Web</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="8.0.0" />
    <PackageReference Include="Microsoft.AspNetCore.Cors" Version="2.2.0" />
    <PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
  </ItemGroup>

</Project>
```

### 3.2 Create Firebase Configuration

Create `Web/ICCMS-Web/wwwroot/js/firebase-config.js`:

```javascript
// Firebase configuration
const firebaseConfig = {
  apiKey: "your-api-key",
  authDomain: "your-project-id.firebaseapp.com",
  projectId: "your-project-id",
  storageBucket: "your-project-id.appspot.com",
  messagingSenderId: "your-sender-id",
  appId: "your-app-id",
};

// Initialize Firebase
firebase.initializeApp(firebaseConfig);

// Initialize Firebase Authentication and Firestore
const auth = firebase.auth();
const db = firebase.firestore();
```

### 3.3 Create Authentication Service

Create `Web/ICCMS-Web/wwwroot/js/auth-service.js`:

```javascript
class AuthService {
  constructor() {
    this.auth = firebase.auth();
    this.currentUser = null;
    this.initAuthStateListener();
  }

  initAuthStateListener() {
    this.auth.onAuthStateChanged((user) => {
      if (user) {
        this.currentUser = user;
        this.onUserSignedIn(user);
      } else {
        this.currentUser = null;
        this.onUserSignedOut();
      }
    });
  }

  async signInWithEmail(email, password) {
    try {
      const userCredential = await this.auth.signInWithEmailAndPassword(
        email,
        password
      );
      return userCredential.user;
    } catch (error) {
      throw error;
    }
  }

  async signUpWithEmail(email, password, displayName) {
    try {
      const userCredential = await this.auth.createUserWithEmailAndPassword(
        email,
        password
      );
      await userCredential.user.updateProfile({
        displayName: displayName,
      });
      return userCredential.user;
    } catch (error) {
      throw error;
    }
  }

  async signOut() {
    try {
      await this.auth.signOut();
    } catch (error) {
      throw error;
    }
  }

  async getCurrentUserToken() {
    if (this.currentUser) {
      return await this.currentUser.getIdToken();
    }
    return null;
  }

  onUserSignedIn(user) {
    // Handle user signed in
    console.log("User signed in:", user.email);
    this.updateUIForAuthenticatedUser(user);
  }

  onUserSignedOut() {
    // Handle user signed out
    console.log("User signed out");
    this.updateUIForUnauthenticatedUser();
  }

  updateUIForAuthenticatedUser(user) {
    // Update UI elements for authenticated user
    const authElements = document.querySelectorAll(".auth-required");
    authElements.forEach((element) => {
      element.style.display = "block";
    });

    const unauthElements = document.querySelectorAll(".auth-not-required");
    unauthElements.forEach((element) => {
      element.style.display = "none";
    });

    // Update user info
    const userInfoElement = document.getElementById("user-info");
    if (userInfoElement) {
      userInfoElement.textContent = `Welcome, ${
        user.displayName || user.email
      }`;
    }
  }

  updateUIForUnauthenticatedUser() {
    // Update UI elements for unauthenticated user
    const authElements = document.querySelectorAll(".auth-required");
    authElements.forEach((element) => {
      element.style.display = "none";
    });

    const unauthElements = document.querySelectorAll(".auth-not-required");
    unauthElements.forEach((element) => {
      element.style.display = "block";
    });
  }
}

// Initialize auth service
const authService = new AuthService();
```

### 3.4 Create API Service

Create `Web/ICCMS-Web/wwwroot/js/api-service.js`:

```javascript
class ApiService {
  constructor() {
    this.baseUrl = "https://localhost:7000/api";
  }

  async getAuthHeaders() {
    const token = await authService.getCurrentUserToken();
    return {
      "Content-Type": "application/json",
      Authorization: token ? `Bearer ${token}` : "",
    };
  }

  async get(endpoint) {
    try {
      const headers = await this.getAuthHeaders();
      const response = await fetch(`${this.baseUrl}${endpoint}`, {
        method: "GET",
        headers: headers,
      });
      return await response.json();
    } catch (error) {
      console.error("API GET Error:", error);
      throw error;
    }
  }

  async post(endpoint, data) {
    try {
      const headers = await this.getAuthHeaders();
      const response = await fetch(`${this.baseUrl}${endpoint}`, {
        method: "POST",
        headers: headers,
        body: JSON.stringify(data),
      });
      return await response.json();
    } catch (error) {
      console.error("API POST Error:", error);
      throw error;
    }
  }

  async put(endpoint, data) {
    try {
      const headers = await this.getAuthHeaders();
      const response = await fetch(`${this.baseUrl}${endpoint}`, {
        method: "PUT",
        headers: headers,
        body: JSON.stringify(data),
      });
      return await response.json();
    } catch (error) {
      console.error("API PUT Error:", error);
      throw error;
    }
  }

  async delete(endpoint) {
    try {
      const headers = await this.getAuthHeaders();
      const response = await fetch(`${this.baseUrl}${endpoint}`, {
        method: "DELETE",
        headers: headers,
      });
      return await response.json();
    } catch (error) {
      console.error("API DELETE Error:", error);
      throw error;
    }
  }
}

// Initialize API service
const apiService = new ApiService();
```

### 3.5 Update Layout

Update `Web/ICCMS-Web/Views/Shared/_Layout.cshtml`:

```html
<!DOCTYPE html>
<html lang="en">
  <head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>@ViewData["Title"] - ICCMS</title>
    <link rel="stylesheet" href="~/lib/bootstrap/dist/css/bootstrap.min.css" />
    <link rel="stylesheet" href="~/css/site.css" asp-append-version="true" />
    <link
      rel="stylesheet"
      href="~/ICCMS_Web.styles.css"
      asp-append-version="true"
    />
  </head>
  <body>
    <header>
      <nav
        class="navbar navbar-expand-sm navbar-toggleable-sm navbar-light bg-white border-bottom box-shadow mb-3"
      >
        <div class="container-fluid">
          <a
            class="navbar-brand"
            asp-area=""
            asp-controller="Home"
            asp-action="Index"
            >ICCMS</a
          >
          <button
            class="navbar-toggler"
            type="button"
            data-bs-toggle="collapse"
            data-bs-target=".navbar-collapse"
          >
            <span class="navbar-toggler-icon"></span>
          </button>
          <div
            class="navbar-collapse collapse d-sm-inline-flex justify-content-between"
          >
            <ul class="navbar-nav flex-grow-1">
              <li class="nav-item">
                <a
                  class="nav-link text-dark"
                  asp-area=""
                  asp-controller="Home"
                  asp-action="Index"
                  >Home</a
                >
              </li>
              <li class="nav-item auth-required" style="display: none;">
                <a
                  class="nav-link text-dark"
                  asp-area=""
                  asp-controller="Projects"
                  asp-action="Index"
                  >Projects</a
                >
              </li>
              <li class="nav-item auth-required" style="display: none;">
                <a
                  class="nav-link text-dark"
                  asp-area=""
                  asp-controller="Maintenance"
                  asp-action="Index"
                  >Maintenance</a
                >
              </li>
            </ul>
            <ul class="navbar-nav">
              <li class="nav-item auth-not-required">
                <button
                  class="btn btn-outline-primary"
                  onclick="showLoginModal()"
                >
                  Login
                </button>
              </li>
              <li class="nav-item auth-required" style="display: none;">
                <span id="user-info" class="navbar-text"></span>
              </li>
              <li class="nav-item auth-required" style="display: none;">
                <button
                  class="btn btn-outline-danger"
                  onclick="authService.signOut()"
                >
                  Logout
                </button>
              </li>
            </ul>
          </div>
        </div>
      </nav>
    </header>

    <div class="container">
      <main role="main" class="pb-3">@RenderBody()</main>
    </div>

    <footer class="border-top footer text-muted">
      <div class="container">&copy; 2024 - ICCMS</div>
    </footer>

    <!-- Firebase SDK -->
    <script src="https://www.gstatic.com/firebasejs/9.0.0/firebase-app-compat.js"></script>
    <script src="https://www.gstatic.com/firebasejs/9.0.0/firebase-auth-compat.js"></script>
    <script src="https://www.gstatic.com/firebasejs/9.0.0/firebase-firestore-compat.js"></script>

    <!-- Custom Scripts -->
    <script src="~/js/firebase-config.js"></script>
    <script src="~/js/auth-service.js"></script>
    <script src="~/js/api-service.js"></script>

    <script src="~/lib/jquery/dist/jquery.min.js"></script>
    <script src="~/lib/bootstrap/dist/js/bootstrap.bundle.min.js"></script>
    <script src="~/js/site.js" asp-append-version="true"></script>
    @await RenderSectionAsync("Scripts", required: false)
  </body>
</html>
```

### 3.6 Create Authentication Views

Create `Web/ICCMS-Web/Views/Auth/Login.cshtml`:

```html
@{ ViewData["Title"] = "Login"; }

<div class="row justify-content-center">
  <div class="col-md-6">
    <div class="card">
      <div class="card-header">
        <h3>Login</h3>
      </div>
      <div class="card-body">
        <form id="loginForm">
          <div class="mb-3">
            <label for="email" class="form-label">Email</label>
            <input type="email" class="form-control" id="email" required />
          </div>
          <div class="mb-3">
            <label for="password" class="form-label">Password</label>
            <input
              type="password"
              class="form-control"
              id="password"
              required
            />
          </div>
          <button type="submit" class="btn btn-primary">Login</button>
        </form>
        <div class="mt-3">
          <p>
            Don't have an account?
            <a href="#" onclick="showSignupModal()">Sign up</a>
          </p>
        </div>
      </div>
    </div>
  </div>
</div>

<script>
  document.getElementById("loginForm").addEventListener("submit", async (e) => {
    e.preventDefault();

    const email = document.getElementById("email").value;
    const password = document.getElementById("password").value;

    try {
      await authService.signInWithEmail(email, password);
      window.location.href = "/";
    } catch (error) {
      alert("Login failed: " + error.message);
    }
  });
</script>
```

## Part 4: Security Rules Setup

### 4.1 Firestore Security Rules

In Firebase Console, go to **Firestore Database** → **Rules** and set:

```javascript
rules_version = '2';
service cloud.firestore {
  match /databases/{database}/documents {
    // Users can read/write their own data
    match /users/{userId} {
      allow read, write: if request.auth != null && request.auth.uid == userId;
    }

    // Project managers can read/write projects they manage
    match /projects/{projectId} {
      allow read, write: if request.auth != null &&
        (resource.data.projectManagerId == request.auth.uid ||
         request.auth.token.role == 'admin');
    }

    // Maintenance requests - clients can create, project managers can read/write
    match /maintenanceRequests/{requestId} {
      allow create: if request.auth != null;
      allow read, write: if request.auth != null &&
        (resource.data.clientId == request.auth.uid ||
         request.auth.token.role == 'projectManager' ||
         request.auth.token.role == 'admin');
    }

    // Quotations - admins can approve, others can read if involved
    match /quotations/{quotationId} {
      allow read: if request.auth != null &&
        (resource.data.clientId == request.auth.uid ||
         resource.data.adminApproverUserId == request.auth.uid ||
         request.auth.token.role == 'admin');
      allow write: if request.auth != null &&
        request.auth.token.role == 'admin';
    }
  }
}
```

## Part 5: Testing and Deployment

### 5.1 Test Your Implementation

1. **Start the API:**

   ```bash
   cd API/ICCMS-API
   dotnet run
   ```

2. **Start the Web Application:**

   ```bash
   cd Web/ICCMS-Web
   dotnet run
   ```

3. **Test Authentication:**

   - Navigate to the web application
   - Try signing up with a new account
   - Test login functionality
   - Verify protected routes are working

4. **Test API Endpoints:**
   - Use Swagger UI at `https://localhost:7000/swagger`
   - Test CRUD operations for users, projects, etc.

### 5.2 Environment Variables

For production, move sensitive configuration to environment variables:

```bash
# API Environment Variables
FIREBASE_PROJECT_ID=your-project-id
FIREBASE_PRIVATE_KEY_ID=your-private-key-id
FIREBASE_PRIVATE_KEY=your-private-key
FIREBASE_CLIENT_EMAIL=your-client-email
FIREBASE_CLIENT_ID=your-client-id
```

## Next Steps

1. **Implement remaining controllers** for all entities in your ERD
2. **Add validation** using FluentValidation
3. **Implement role-based authorization**
4. **Add file upload functionality** for documents
5. **Implement real-time notifications** using Firebase Cloud Messaging
6. **Add error handling and logging**
7. **Set up CI/CD pipeline**
8. **Configure production environment**

This implementation guide provides a solid foundation for your ICCMS system with Firebase integration. The architecture follows best practices for security, scalability, and maintainability.
