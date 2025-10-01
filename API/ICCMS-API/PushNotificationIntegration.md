# Push Notification Integration for Messaging

This document describes the push notification integration that has been added to the messaging endpoints in the ICCMS API.

## Overview

The messaging system now includes Firebase Cloud Messaging (FCM) push notifications that are automatically sent when:

- A new message is created (individual message)
- A broadcast message is sent to multiple users

## Changes Made

### 1. User Model Updates

- Added `DeviceToken` property to the `User` model to store FCM device tokens

### 2. MessagesController Updates

- Injected `INotificationService` dependency
- Modified `CreateMessage` endpoint to send push notifications
- Added `SendMessageNotificationAsync` private method for notification logic
- Added `BroadcastMessage` endpoint for sending messages to multiple users

### 3. UsersController Updates

- Added `UpdateDeviceToken` endpoint for mobile apps to register device tokens

## API Endpoints

### Update Device Token

```
PUT /api/users/device-token
Authorization: Bearer <token>
Content-Type: application/json

{
  "deviceToken": "fcm_device_token_here"
}
```

### Create Message (with push notification)

```
POST /api/messages
Authorization: Bearer <token>
Content-Type: application/json

{
  "senderId": "user123",
  "receiverId": "user456",
  "projectId": "project789",
  "subject": "Message Subject",
  "content": "Message content here"
}
```

### Broadcast Message

```
POST /api/messages/broadcast
Authorization: Bearer <token>
Content-Type: application/json

{
  "senderId": "user123",
  "projectId": "project789",
  "subject": "Broadcast Subject",
  "content": "Broadcast message content"
}
```

## Notification Data Structure

Push notifications include the following custom data:

- `messageId`: Unique identifier for the message
- `senderId`: ID of the user who sent the message
- `receiverId`: ID of the user receiving the message (for individual messages)
- `projectId`: ID of the associated project
- `type`: "message" for individual messages, "broadcast" for broadcast messages
- `action`: "message_received" or "broadcast_message"

## Mobile App Integration

### 1. Register Device Token

When the mobile app starts or when the user logs in, call the device token update endpoint:

```javascript
// Example for React Native
import messaging from "@react-native-firebase/messaging";

const registerDeviceToken = async (userId) => {
  try {
    const token = await messaging().getToken();
    await fetch("/api/users/device-token", {
      method: "PUT",
      headers: {
        Authorization: `Bearer ${authToken}`,
        "Content-Type": "application/json",
      },
      body: JSON.stringify({ deviceToken: token }),
    });
  } catch (error) {
    console.error("Failed to register device token:", error);
  }
};
```

### 2. Handle Incoming Notifications

Set up notification handlers in your mobile app:

```javascript
// Handle background messages
messaging().setBackgroundMessageHandler(async (remoteMessage) => {
  console.log("Message handled in the background!", remoteMessage);
});

// Handle foreground messages
const unsubscribe = messaging().onMessage(async (remoteMessage) => {
  // Show in-app notification
  Alert.alert(
    remoteMessage.notification.title,
    remoteMessage.notification.body
  );
});
```

## Error Handling

- If a user doesn't have a device token registered, the message will still be created but no push notification will be sent
- Push notification failures are logged but don't affect message creation
- The system gracefully handles cases where FCM is unavailable

## Testing

To test the integration:

1. Register a device token for a test user
2. Send a message to that user
3. Verify the push notification is received
4. Test broadcast messages to multiple users

## Security Considerations

- Device tokens are only accessible to the user who owns them
- Push notifications are only sent to the intended recipient
- All endpoints require proper authentication
