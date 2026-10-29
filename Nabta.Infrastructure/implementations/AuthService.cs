using Azure.Core;
using Infrastructure.Helpers;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Nabta.Application;
using Nabta.Application.Dtos.Auth;
using Nabta.Application.Featuers.Services;
using Nabta.Infrastructure.Data;
using Nabta.Infrastructure.identity;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Nabta.Infrastructure.Implementations
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly JwtOptions _jwtOptions;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly AppDbContext _context;


        public AuthService(UserManager<ApplicationUser> userManager , IOptions<JwtOptions> options, SignInManager<ApplicationUser> signInManager, AppDbContext context)
        {
            _userManager = userManager;
            _jwtOptions = options.Value;
            _signInManager = signInManager;
            _context = context;
        }
        
        
        public async Task<AuthModel> LoginAsync(LoginDto loginDto)
        {
            ApplicationUser? user;

            if (loginDto.EmailOrPhone.Contains('@'))
            {
                // Login using Email
                user = await _userManager.FindByEmailAsync(loginDto.EmailOrPhone);
            }
            else
            {
                // Login using Phone
                user = await _userManager.Users
                    .FirstOrDefaultAsync(u => u.PhoneNumber == loginDto.EmailOrPhone);
            }

            if (user == null || !await _userManager.CheckPasswordAsync(user, loginDto.Password))
            {
                return new AuthModel
                {
                    Message = "Email/Phone or Password is not valid"
                };
            }

            var jwtToken = await GenerateToken(user);
            var rolesList = await _userManager.GetRolesAsync(user);

            return new AuthModel
            {
                ExpiresOn = jwtToken.ValidTo,
                Email = loginDto.EmailOrPhone.Contains('@')? loginDto.EmailOrPhone: null,
                PhoneNumber = loginDto.EmailOrPhone.Contains('@')? null : loginDto.EmailOrPhone,
                IsAuthenticated = true,
                Roles = rolesList.ToList(),
                Token = new JwtSecurityTokenHandler().WriteToken(jwtToken),
                Message = "Welcome again ^_^"
            };

        }

        public async Task<AuthModel> RegisterAsync(RegisterDto dto)
        {
            // 1. Check if email exists (only if provided)
            ApplicationUser? foundMail = null;

            if (!string.IsNullOrWhiteSpace(dto.Email))
            {
                foundMail = await _userManager.FindByEmailAsync(dto.Email.Trim());
            }

            // 2. Check if phone number exists
            var phoneNumber = dto.PhoneNumber.Trim();

            var foundByPhone = await _userManager.Users
                .FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber);

            // 3. Check if email or phone already exists
            if (foundMail != null || foundByPhone != null)
            {
                return new AuthModel
                {
                    Message = "Email or PhoneNumber already exists."
                };
            }

            // 4. Create new user
            var newAppUser = new ApplicationUser
            {
                FullName = dto.FullName.Trim(),
                UserName = phoneNumber,
                PhoneNumber = phoneNumber,

                Email = string.IsNullOrWhiteSpace(dto.Email)
                    ? null
                    : dto.Email.Trim()
            };

            // 5. Create user and hash password
            var result = await _userManager.CreateAsync(
                newAppUser,
                dto.Password);

            if (!result.Succeeded)
            {
                var errorList = string.Join(" ", result.Errors.Select(error => error.Description));

                return new AuthModel
                {
                    Message = $"There are some errors: {errorList}"
                };
            }

            // 6. Assign role
            var roleResult = await _userManager.AddToRoleAsync(newAppUser,"user");

            if (!roleResult.Succeeded)
            {
                var errorList = string.Join(" ",roleResult.Errors.Select(error => error.Description));

                await _userManager.DeleteAsync(newAppUser);

                return new AuthModel
                {
                    Message = $"Could not assign role: {errorList}"
                };
            }

            // 7. Generate JWT
            var token = await GenerateToken(newAppUser);

            // 8. Return response
            return new AuthModel
            {
                Email = newAppUser.Email,
                PhoneNumber = newAppUser.PhoneNumber,
                ExpiresOn = token.ValidTo,
                IsAuthenticated = true,
                Roles = new List<string> { "user" },
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                Username = newAppUser.UserName
            };
        }

        public async Task SignOut()
        {
           await _signInManager.SignOutAsync();
        }

        // Generate Tokens
        private async Task<SecurityToken> GenerateToken(ApplicationUser user)
        {
            var userClaims = await _userManager.GetClaimsAsync(user);
            var roles = await _userManager.GetRolesAsync(user);

            var roleClaims = roles
                .Select(role => new Claim(ClaimTypes.Role, role))
                .ToList();

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.UserName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email!),
                new Claim("userID" , user.Id.ToString())
            }
            .Union(userClaims)
            .Union(roleClaims);

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_jwtOptions.Key)
            );

            var creds = new SigningCredentials(
                key,
                SecurityAlgorithms.HmacSha256
            );

            var token = new JwtSecurityToken(
                issuer: _jwtOptions.Issuer,
                audience: _jwtOptions.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddDays(_jwtOptions.ExpirationInDays),
                signingCredentials: creds
            );

            return token;
        }

        public async Task<AuthModel> RefreshTokenAsync(string token)
        {
            var authModel = new AuthModel();
            var user = await _userManager.Users.SingleOrDefaultAsync(u => u.RefreshTokens.Any(t => t.Token == token));

            if (user == null)
            {
                authModel.IsAuthenticated = false;
                authModel.Message = "Invalid Token";
                return authModel;
            }

            var refreshToken = user.RefreshTokens.Single(t => t.Token == token);

            if (!refreshToken.IsActive)
            {
                authModel.IsAuthenticated = false;
                authModel.Message = "Inactive Token";
                return authModel;
            }

            refreshToken.RevokedOn = DateTime.UtcNow;
            var newRefreshToken = await GenerateRefreshToken();
            user.RefreshTokens.Add(newRefreshToken);
            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                authModel.IsAuthenticated = false;
                authModel.Message = "Failed to update token";
                return authModel;
            }

            var jwt = await GenerateToken(user);
            authModel.IsAuthenticated = true;
            authModel.Message = "Successfully";
            authModel.Email = user.Email;
            authModel.PhoneNumber = user.PhoneNumber;
            authModel.Username = user.UserName;
            authModel.Roles = (await _userManager.GetRolesAsync(user)).ToList();
            authModel.Token = new JwtSecurityTokenHandler().WriteToken(jwt);
            authModel.RefreshToken = newRefreshToken.Token;
            authModel.RefreshTokenExpiration = newRefreshToken.ExpiresOn;

            return authModel;
        }

        public async Task<bool> RevokeToken(string token)
        {
            var authModel = new AuthModel();
            var user = await _userManager.Users.SingleOrDefaultAsync(u => u.RefreshTokens.Any(t => t.Token == token));

            if (user == null)
                return false;

            var refreshToken = user.RefreshTokens.Single(t => t.Token == token);

            if (!refreshToken.IsActive)
                return false;

            refreshToken.RevokedOn = DateTime.UtcNow;
            var newRefreshToken = await GenerateRefreshToken();
            user.RefreshTokens.Add(newRefreshToken);
            var result = await _userManager.UpdateAsync(user);

            return true;
        }

        private async Task<RefreshToken> GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            var generator = new RNGCryptoServiceProvider();

            return new RefreshToken
            {
                Token = Convert.ToBase64String(randomNumber),
                ExpiresOn = DateTime.UtcNow.AddDays(10),
                CreatedOn = DateTime.UtcNow
            };
        }

    }
}
