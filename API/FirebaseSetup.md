# Firebase Setup Guide for ICCMS Developers

## Overview

This guide explains how to set up Firebase credentials securely using .NET User Secrets. This approach keeps sensitive Firebase credentials out of version control while maintaining proper functionality.

## Prerequisites

- .NET 8.0 SDK
- Firebase service account key (JSON file) - use the one shared in the group chat

## Step 1: Initialize User Secrets

1. Navigate to your API project directory:

   ```bash
   cd API/ICCMS-API
   ```

2. Initialize user secrets:
   ```bash
   dotnet user-secrets init
   ```

## Step 2: Add Firebase Credentials Path

Add the path to your Firebase service account file to user secrets:

```bash
dotnet user-secrets set "Firebase:CredentialsPath" "path-to-service-account"
```

**Important**: Replace `[path-to-service-account]` with the path points to where you stored the service account file shared in the group chat.

## Step 3: Verify Setup

1. Start your API application:

   ```bash
   dotnet run
   ```

2. Check the console output for:
   ```
   FirestoreDb created successfully
   ```

## Troubleshooting

- **File not found error**: Ensure the path in the user secrets command matches exactly where you stored the service account file
- **Permission error**: Make sure you have read access to the file location
- **Invalid credentials**: Verify you're using the correct service account file shared in the group chat

## Security Note

- User secrets are automatically ignored by git
- The service account file should be stored outside the project directory
- Never commit the service account file to version control
