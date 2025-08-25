using Google.Cloud.Firestore;

namespace ICCMS_Web.Models
{
    [FirestoreData]
    public class User
    {
        [FirestoreProperty("userId")]
        public string UserId { get; set; } = string.Empty;

        [FirestoreProperty("role")]
        public string Role { get; set; } = string.Empty;

        [FirestoreProperty("fullName")]
        public string FullName { get; set; } = string.Empty;

        [FirestoreProperty("email")]
        public string Email { get; set; } = string.Empty;

        [FirestoreProperty("phone")]
        public string Phone { get; set; } = string.Empty;

        [FirestoreProperty("isActive")]
        public bool IsActive { get; set; }

        [FirestoreProperty("createdAt")]
        public DateTime CreatedAt { get; set; }
    }
}
