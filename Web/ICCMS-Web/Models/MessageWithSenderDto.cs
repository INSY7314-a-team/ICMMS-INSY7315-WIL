using System.Text.Json.Serialization;

namespace ICCMS_Web.Models
{
    public class MessageWithSenderDto : MessageDto
    {
        [JsonPropertyName("senderName")]
        public string SenderName { get; set; } = "Unknown User";
    }
}
