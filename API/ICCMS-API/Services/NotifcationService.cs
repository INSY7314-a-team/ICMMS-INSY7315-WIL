namespace ICCMS_API.Services;

public class FcmNotificationService : IFcmNotificationService
{
    public async Task<string> SendToDeviceAsync(
        string deviceToken,
        string title,
        string body,
        Dictionary<string, string>? data = null
    )
    {
        var message = new Message()
        {
            Token = deviceToken,
            Notification = new Notification { Title = title, Body = body },
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
        var message = new MulticastMessage()
        {
            Tokens = deviceTokens,
            Notification = new Notification { Title = title, Body = body },
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
        var message = new Message()
        {
            Topic = topic, // e.g., "project-123" or "contractors"
            Notification = new Notification { Title = title, Body = body },
            Data = data ?? new Dictionary<string, string>(),
        };

        string response = await FirebaseMessaging.DefaultInstance.SendAsync(message);
        return response;
    }
}
