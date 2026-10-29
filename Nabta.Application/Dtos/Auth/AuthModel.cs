using System.Text.Json.Serialization;

namespace Nabta.Application
{
    public class AuthModel
    {
        public string? Message { get; set; }

        public bool IsAuthenticated { get; set; }

        public string? UserId { get; set; }

        public string? Username { get; set; }

        public string? Email { get; set; }

        public string? PhoneNumber { get; set; }

        public string? Token { get; set; }

        public DateTime ExpiresOn { get; set; }

        public string? RefreshToken { get; set; }

        public DateTime? RefreshTokenExpiration { get; set; }

        public IList<string> Roles { get; set; } = new List<string>();
    }
}
