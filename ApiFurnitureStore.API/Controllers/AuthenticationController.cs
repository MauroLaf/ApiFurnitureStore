using ApiFurnitureStore.API.Configuration;
using ApiFurnitureStore.Data;
using ApiFurnitureStore.Shared.Auth;
using ApiFurnitureStore.Shared.Common;
using ApiFurnitureStore.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;

namespace ApiFurnitureStore.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthenticationController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly JwtConfig _jwtConfig;
        private readonly IEmailSender _emailSender;
        private readonly ApiFurnitureStoreContext _context;
        private readonly TokenValidationParameters _tokenValidationParameters;
        private readonly ILogger<AuthenticationController> _logger;



        public AuthenticationController(UserManager<IdentityUser> userManager,
                                        IOptions<JwtConfig> jwtConfig,
                                        IEmailSender emailSender,
                                        ApiFurnitureStoreContext context,
                                        TokenValidationParameters tokenValidationParameters,
                                        ILogger<AuthenticationController> logger)
        {
            _userManager = userManager;
            _jwtConfig = jwtConfig.Value; //es de tipo jwtconfig si dejo sin value me marcara error
            _emailSender = emailSender;
            _context = context;
            _tokenValidationParameters = tokenValidationParameters;
            _logger = logger;
        }
        //creo endpoint de registro
        [HttpPost("Register")]
        public async Task<IActionResult> Register([FromBody] UserRegistrationRequestDto request)
        {
            //agrego un warning para probar el logger que inyecte en este controlador
            _logger.LogWarning("A user trying to register");

            if (!ModelState.IsValid) return BadRequest();//verifico el estado

            //verifico si existe el mail
            var emailExists = await _userManager.FindByEmailAsync(request.EmailAddress);
            if (emailExists != null)
                return BadRequest(new AuthResult()
                {
                    Result = false,
                    Errors = new List<string>()
                    {
                        "Email already exists"
                    }
                });

            //creamos user
            var user = new IdentityUser() //usamos la clase que nos da la libreria directamente
            {
                Email = request.EmailAddress,
                UserName = request.EmailAddress,
                EmailConfirmed = false //agrego al tener el mail de confirmacion
            };

            var isCreated = await _userManager.CreateAsync(user, request.Password);
            if (isCreated.Succeeded)
            {
                //var token = GenerateToken(user);
                await SendVerificationEmail(user);
                return Ok(new AuthResult()
                {
                    Result = true,

                    // Token = token
                });
            }
            else
            {
                var errors = new List<string>();
                foreach (var err in isCreated.Errors)
                    errors.Add(err.Description);

                return BadRequest(new AuthResult
                {
                    Result = false,
                    Errors = errors
                });
            }
            //OPCIONAL
            return BadRequest(new AuthResult
            {
                Result = false,
                Errors = new List<string> { "User couldn't be created" }
            });
        }
        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody] UserLoginRequestDto request)
        {
            if (!ModelState.IsValid) return BadRequest();

            // 1. Busco el usuario
            var existingUser = await _userManager.FindByEmailAsync(request.Email);

            // 2. Si no existe, devuelvo error
            if (existingUser == null)

                return BadRequest(new AuthResult
                {
                    Errors = new List<string> { "Invalid Payload" },
                    Result = false
                });

            if (!existingUser.EmailConfirmed)
                return BadRequest(new AuthResult
                {
                    Errors = new List<string> { "email needs to be confirmed." },
                    Result = false
                });


            // 3. Verifico la contrase�a
            var checkUserAndPass = await _userManager.CheckPasswordAsync(existingUser, request.Password);
            if (!checkUserAndPass)
            {
                return BadRequest(
                    new AuthResult
                    {
                        Errors = new List<string> { "Invalid Credentials" },
                        Result = false
                    });
            }

            // 4. Si todo est� bien, creo el token
            var token = GenerateTokenAsync(existingUser);
            return Ok(token);

        }

        //este endpoint es para darle un token a todo el que quiera usar la api para hacer un refresh
        [HttpPost("RefreshToken")]
        public async Task<IActionResult> RefreshToken([FromBody] TokenRequest tokenRequest)
        {
            if (!ModelState.IsValid)
                return BadRequest(new AuthResult
                {
                    Errors = new List<string> { "Invalid parameters" },
                    Result = false
                });
            var results = VerifyAndGenerateTokenAsync(tokenRequest);
            if (results == null)
                return BadRequest(new AuthResult
                {
                    Errors = new List<string> { "Invalid token" }
                });
            return Ok(results);
        }

        [HttpGet("ConfirmEmail")]
        public async Task<IActionResult> ConfirmEmail(string userId, string code)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(code))
                return BadRequest(new AuthResult
                {
                    Errors = new List<string> { "Invalid email confirmation url" },
                    Result = false
                });
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound($"Unable to load user with Id '{userId}'.");

            code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));

            var result = await _userManager.ConfirmEmailAsync(user, code);

            var status = result.Succeeded ? "Thank you for confirming your email."
                                            : "There has been an error confirming your email.";

            return Ok(status);

        }
        //creo la clase token
        private async Task<AuthResult> GenerateTokenAsync(IdentityUser user)
        {
            var jwtTokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_jwtConfig.Secret);
            var tokenDescriptor = new SecurityTokenDescriptor()
            {
                Subject = new ClaimsIdentity(new ClaimsIdentity(new[]
                {
                    new Claim("Id", user.Id),
                    new Claim(JwtRegisteredClaimNames.Sub, user.Email),
                    new Claim(JwtRegisteredClaimNames.Email, user.Email),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                    new Claim(JwtRegisteredClaimNames.Iat, DateTime.Now.ToUniversalTime().ToString())
                })),
                Expires = DateTime.UtcNow.Add(_jwtConfig.ExpiryTime),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256)//cambie el alg
            };
            var token = jwtTokenHandler.CreateToken(tokenDescriptor);
            var jwtToken = jwtTokenHandler.WriteToken(token);
            var refreshToken = new RefreshToken
            {
                JwtId = token.Id,
                Token = RandomGenerator.GenerateRandomString(23),
                AddedDate = DateTime.UtcNow,
                ExpiryDate = DateTime.UtcNow.AddMonths(6),
                IsRevoked = false,
                IsUsed = false,
                UserId = user.Id,
            };

            await _context.RefreshTokens.AddAsync(refreshToken);
            await _context.SaveChangesAsync();

            return new AuthResult
            {
                Token = jwtToken,
                RefreshToken = refreshToken.Token,
                Result = true
            };
        }
        private async Task SendVerificationEmail(IdentityUser user)
        {
            var verificationCode = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            verificationCode = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(verificationCode));

            //example: https://localhost:5015/api/authentication/verifyemail/userId=exampleuserId&code=examplecode
            var callbackUrl = $@"{Request.Scheme}://{Request.Host}{Url.Action("ConfirmEmail", controller: "Authentication", new { userId = user.Id, code = verificationCode })}";

            var emailBody = $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>";
            await _emailSender.SendEmailAsync(user.Email, "Confirm your Email", emailBody);
        }
        private async Task <AuthResult> VerifyAndGenerateTokenAsync(TokenRequest tokenRequest)
        {
            var jwtTokenHandler = new JwtSecurityTokenHandler();

            try
            {
                _tokenValidationParameters.ValidateLifetime = false;

                var tokenBeingVerified = jwtTokenHandler.ValidateToken(tokenRequest.Token, _tokenValidationParameters, out var validatedToken);

                if(validatedToken is JwtSecurityToken jwtSecurityToken)
                {
                    var result = jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256,
                        StringComparison.InvariantCultureIgnoreCase);

                    if (!result || tokenBeingVerified == null)
                        throw new Exception("Invalid Token");
                }

                var utcExpiryDate = long.Parse(tokenBeingVerified.Claims.
                                    FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Exp).Value);

                var expiryDate = DateTimeOffset.FromUnixTimeSeconds(utcExpiryDate).UtcDateTime;

                if(expiryDate < DateTimeOffset.UtcNow)
                    throw new Exception("Token Expired");

                var storedToken = await _context.RefreshTokens.
                    FirstOrDefaultAsync(t => t.Token == tokenRequest.RefreshToken);

                if(storedToken == null)
                    throw new Exception("Invalid Token");

                if(storedToken.IsUsed || storedToken.IsRevoked)
                    throw new Exception("Invalid Token");

                var jti = tokenBeingVerified.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti).Value;

                if (jti != storedToken.JwtId)
                    throw new Exception("Invalid Token");

                if (storedToken.ExpiryDate < DateTime.UtcNow)
                    throw new Exception("Token Expired");
                storedToken.IsUsed = true;
                _context.RefreshTokens.Update(storedToken);
                await _context.SaveChangesAsync();

                var dbUser = await _userManager.FindByIdAsync(storedToken.UserId);
                return await GenerateTokenAsync(dbUser);

            }
            catch(Exception e)
            {
                var message = e.Message == "Invalid Token" || e.Message == "Token Expired"
                    ? e.Message
                    : "Internal Server Error";
                return new AuthResult() { Result = false, Errors = new List<string> { message } };
            }
        }
    }
}
