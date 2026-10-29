using System.ComponentModel.DataAnnotations;

namespace Nabta.Application.Dtos.Auth
{

    public class LoginDto
    {
        [Required]
        public string EmailOrPhone { get; set; }
        [Required]
        public string Password { get; set; }
    }


}