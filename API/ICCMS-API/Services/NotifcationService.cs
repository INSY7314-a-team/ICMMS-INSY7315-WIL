using System.Collections.Generic;
using System.Threading.Tasks;
using FirebaseAdmin.Messaging;
using ICCMS_API.Models;

namespace ICCMS_API.Services;

public class NotificationService : INotificationService
{
    public async Task<string> SendToDeviceAsync(
        string deviceToken,
        string title,
        string body,
        Dictionary<string, string>? data = null
    )
    {
        var message = new FirebaseAdmin.Messaging.Message()
        {
            Token = deviceToken,
            Notification = new FirebaseAdmin.Messaging.Notification { Title = title, Body = body },
            Data = data ?? new Dictionary<string, string>(), // Custom data, e.g., { "projectId": "123", "action": "taskAssigned" }
        };

        string response = await FirebaseMessaging.DefaultInstance.SendAsync(message);
        return response; // Message ID on success
    }

    public async Task<BatchResponse> SendToMultipleDevicesAsync(
        List<string> deviceTokens,
        string title,
        string body,
        Dictionary<string, string>? data = null
    )
    {
        var message = new FirebaseAdmin.Messaging.MulticastMessage()
        {
            Tokens = deviceTokens,
            Notification = new FirebaseAdmin.Messaging.Notification { Title = title, Body = body },
            Data = data ?? new Dictionary<string, string>(),
        };

        return await FirebaseMessaging.DefaultInstance.SendEachForMulticastAsync(message);
    }

    public async Task<string> SendToTopicAsync(
        string topic,
        string title,
        string body,
        Dictionary<string, string>? data = null
    )
    {
        var message = new FirebaseAdmin.Messaging.Message()
        {
            Topic = topic, // e.g., "project-123" or "contractors"
            Notification = new FirebaseAdmin.Messaging.Notification { Title = title, Body = body },
            Data = data ?? new Dictionary<string, string>(),
        };

        string response = await FirebaseMessaging.DefaultInstance.SendAsync(message);
        return response;
    }
}
