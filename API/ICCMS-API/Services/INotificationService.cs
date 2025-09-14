using System.Collections.Generic;
using System.Threading.Tasks;
using FirebaseAdmin.Messaging;

public interface IFcmNotificationService
{
    Task<string> SendToDeviceAsync(
        string deviceToken,
        string title,
        string body,
        Dictionary<string, string>? data = null
    );
    Task<BatchResponse> SendToMultipleDevicesAsync(
        List<string> deviceTokens,
        string title,
        string body,
        Dictionary<string, string>? data = null
    );
    Task<string> SendToTopicAsync(
        string topic,
        string title,
        string body,
        Dictionary<string, string>? data = null
    );
}
