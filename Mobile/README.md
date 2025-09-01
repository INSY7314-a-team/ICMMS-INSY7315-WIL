# ICCMS Mobile Application

This is the mobile application for the Integrated Construction and Maintenance Management System (ICCMS).

## Features Implemented

### Authentication
- **Login Screen**: Clean, modern login interface with email and password fields
- **API Integration**: Connects to the ICCMS API for authentication
- **Error Handling**: Displays appropriate error messages for failed login attempts
- **Loading States**: Shows loading indicators during authentication

### Role-Based Navigation
- **Client Dashboard**: Full-featured dashboard for clients with quick actions
- **Admin Dashboard**: Placeholder dashboard for administrators
- **Project Manager Dashboard**: Placeholder dashboard for project managers
- **Automatic Role Detection**: Automatically navigates users to appropriate dashboard based on their role

### Client Dashboard Features
- **Welcome Section**: Personalized greeting with user information
- **Quick Actions**: Easy access to common client functions:
  - Create new maintenance request
  - View projects
  - View quotations
  - Access messages
- **Recent Activity**: Placeholder for displaying recent activity
- **Logout Functionality**: Secure logout with navigation back to login

## Technical Implementation

### Architecture
- **MVVM Pattern**: Uses ViewModel and StateFlow for state management
- **Repository Pattern**: Separates data access logic
- **Jetpack Compose**: Modern UI framework for Android
- **Navigation Component**: Handles screen navigation and role-based routing

### Dependencies
- **Retrofit**: HTTP client for API communication
- **OkHttp**: Network interceptor for logging
- **Gson**: JSON serialization/deserialization
- **Navigation Compose**: Navigation between screens
- **Material 3**: Modern Material Design components

### API Integration
- **Base URL**: `http://10.0.2.2:5031/` (Android emulator localhost)
- **Authentication Endpoint**: `POST /api/auth/login`
- **Request/Response Models**: Properly typed data classes

## Setup Instructions

1. **API Server**: 
   - Run the API server using `runApi.bat` from the project root
   - The server will run on `http://localhost:5031` (HTTP) for development
   - HTTPS redirection is disabled in development mode to avoid SSL certificate issues
2. **Android Emulator**: Use Android emulator to access localhost (10.0.2.2)
3. **Build**: Run `./gradlew build` to build the application
4. **Install**: Install the APK on your device or emulator

## Testing

To test the login functionality:

1. **Start the API server** (run `runApi.bat` from the project root)
2. **Launch the mobile app** on an Android emulator
3. **Use test credentials** (ensure you have a user account in your Firebase/Firestore database)
4. **Verify role-based navigation** by testing with different user roles

## Future Enhancements

- **Token Storage**: Implement secure token storage using Android Keystore
- **Offline Support**: Add offline capabilities with local data caching
- **Push Notifications**: Integrate Firebase Cloud Messaging
- **Biometric Authentication**: Add fingerprint/face unlock support
- **Dark Mode**: Implement theme switching
- **Additional Dashboards**: Complete implementation of Admin and Project Manager dashboards

## File Structure

```
app/src/main/java/com/example/iccms_mobile/
├── data/
│   ├── api/           # API service interfaces
│   ├── models/        # Data models for API communication
│   ├── network/       # Network configuration (Retrofit)
│   └── repository/    # Data access layer
├── ui/
│   ├── navigation/    # Navigation components
│   ├── screens/       # UI screens (Login, Dashboards)
│   └── viewmodel/     # ViewModels for state management
└── MainActivity.kt    # Main application entry point
```
