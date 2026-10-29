using Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Nabta.Infrastructure.identity
{
    public class ApplicationUser:IdentityUser   
    {
        [Required]
        [MaxLength(150)]
        public string FullName { get; set; } = string.Empty;
        public List<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    }
}
