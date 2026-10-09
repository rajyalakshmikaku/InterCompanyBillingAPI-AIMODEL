using InterCompanyBillingSulution.DataAcess.Data;
using InterCompanyBillingSulution.Models;
using InterCompanyBillingSulution.Models.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace InterCompanyBillingSulution_Core.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LoginController : ControllerBase
    {
        private readonly InterCompanyBillingDbContext _context;
        private readonly IEmailService _emailService;
        private readonly AuditService _auditService;
        private readonly IConfiguration _configuration;
        private readonly PasswordHasher<Users> _passwordHasher = new PasswordHasher<Users>();
        private readonly PasswordEncryptionService _passwordEncryptionService;

        public LoginController(InterCompanyBillingDbContext context, IEmailService emailService, AuditService auditService, IConfiguration configuration, PasswordEncryptionService passwordEncryptionService)
        {
            _context = context;
            _emailService = emailService;
            _auditService = auditService;
            _configuration = configuration;
            _passwordEncryptionService = passwordEncryptionService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            try
            {
                // ============================================================
                // GET CLIENT IP
                // ============================================================

                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

                // ============================================================
                // PREPARE LOGIN HISTORY
                // ============================================================

                var loginHistory = new LoginHistory
                {
                    UserName = model.UserName,
                    IPaddressOfSystem = ipAddress,
                    LoginDateTime = DateTime.Now,
                    IsLocked = "No"
                };

                // ============================================================
                // FIND USER BY USERNAME OR EMAIL
                // ============================================================

                var userDetails = await (
                    from u in _context.Users

                    join r in _context.Roles
                        on u.RoleId equals r.Id into roleGroup

                    from r in roleGroup.DefaultIfEmpty()

                    where
                        u.UserName.Trim() == model.UserName.Trim()
                        ||
                        u.Email.Trim() == model.UserName.Trim()

                    select new
                    {
                        UserId = u.Id,
                        u.UserName,
                        u.Name,
                        u.Email,
                        u.CompanyId,
                        u.Activation,

                        RoleName = r != null
                            ? r.RoleName
                            : null,

                        RoleId = r != null
                            ? r.Id
                            : 0,

                        u.Status,
                        u.Password
                    }
                ).FirstOrDefaultAsync();

                // ============================================================
                // USER NOT FOUND
                // ============================================================

                if (userDetails == null)
                {
                    loginHistory.LoginStatus = "Invalid Credentials";

                    _context.LoginHistory.Add(loginHistory);
                    await _context.SaveChangesAsync();

                    return Unauthorized(new
                    {
                        message = "Invalid Username or Password"
                    });
                }

                // ============================================================
                // GET ACTUAL USER ENTITY
                // IMPORTANT FOR PASSWORD HASH VERIFICATION
                // ============================================================

                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(x => x.Id == userDetails.UserId);

                if (existingUser == null)
                {
                    loginHistory.UserID = userDetails.UserId;
                    loginHistory.LoginStatus = "Invalid Credentials";

                    _context.LoginHistory.Add(loginHistory);
                    await _context.SaveChangesAsync();

                    return Unauthorized(new
                    {
                        message = "Invalid Username or Password"
                    });
                }

                // ============================================================
                // PASSWORD VERIFICATION
                //
                // Supports:
                // 1. ASP.NET Core hashed password
                // 2. Old plain-text password
                // ============================================================

                bool passwordValid = false;
                bool oldPlainTextPassword = false;

                string storedPassword = userDetails.Password ?? "";
                string enteredPassword = model.Password ?? "";

                if (!string.IsNullOrEmpty(storedPassword) &&
                    !string.IsNullOrEmpty(enteredPassword))
                {
                    // ========================================================
                    // CHECK ASP.NET CORE PASSWORD HASH
                    // ========================================================

                    bool isHashedPassword =
                        storedPassword.StartsWith("AQAAAA");

                    if (isHashedPassword)
                    {
                        // ====================================================
                        // VERIFY HASHED PASSWORD
                        // ====================================================

                        try
                        {
                            var passwordResult =
                                _passwordHasher.VerifyHashedPassword(
                                    existingUser,
                                    storedPassword,
                                    enteredPassword
                                );

                            if (passwordResult == PasswordVerificationResult.Success ||
                                passwordResult == PasswordVerificationResult.SuccessRehashNeeded)
                            {
                                passwordValid = true;
                            }
                        }
                        catch
                        {
                            passwordValid = false;
                        }
                    }
                    else
                    {
                        // ====================================================
                        // OLD PLAIN-TEXT PASSWORD
                        // ====================================================

                        if (storedPassword == enteredPassword)
                        {
                            passwordValid = true;
                            oldPlainTextPassword = true;
                        }
                    }
                }

                // ============================================================
                // INVALID PASSWORD
                // ============================================================

                if (!passwordValid)
                {
                    loginHistory.UserID = userDetails.UserId;
                    loginHistory.LoginStatus = "Invalid Credentials";

                    _context.LoginHistory.Add(loginHistory);
                    await _context.SaveChangesAsync();

                    return Unauthorized(new
                    {
                        message = "Invalid Username or Password"
                    });
                }

                // ============================================================
                // CONVERT OLD PLAIN-TEXT PASSWORD TO HASH
                // ============================================================

                if (oldPlainTextPassword)
                {
                    existingUser.Password =
                        _passwordHasher.HashPassword(
                            existingUser,
                            enteredPassword
                        );

                    existingUser.ModifiedDate = DateTime.Now;
                    existingUser.ModifiedBy = userDetails.UserName;

                    await _context.SaveChangesAsync();
                }

                // ============================================================
                // CHECK SUPER ADMIN / ADMIN
                // ============================================================

                var isSuperAdmin =
                    string.Equals(
                        userDetails.RoleName?.Trim(),
                        "SuperAdmin",
                        StringComparison.OrdinalIgnoreCase
                    )
                    ||
                    string.Equals(
                        userDetails.RoleName?.Trim(),
                        "Super Admin",
                        StringComparison.OrdinalIgnoreCase
                    );

                // ============================================================
                // CHECK ACCOUNT DECLINED
                // ============================================================

                if (!isSuperAdmin &&
                    string.Equals(
                        userDetails.Status?.Trim(),
                        "Declined",
                        StringComparison.OrdinalIgnoreCase))
                {
                    loginHistory.UserID = userDetails.UserId;
                    loginHistory.LoginStatus = "Account Declined";

                    _context.LoginHistory.Add(loginHistory);
                    await _context.SaveChangesAsync();

                    return Unauthorized(new
                    {
                        message =
                            "Your registration has been declined. Please contact the Super Admin for further assistance."
                    });
                }

                // ============================================================
                // CHECK ACCOUNT STATUS
                // ============================================================

                if (!isSuperAdmin &&
                    !string.Equals(
                        userDetails.Status?.Trim(),
                        "Approved",
                        StringComparison.OrdinalIgnoreCase))
                {
                    loginHistory.UserID = userDetails.UserId;
                    loginHistory.LoginStatus = "Account is not approved";

                    _context.LoginHistory.Add(loginHistory);
                    await _context.SaveChangesAsync();

                    return Unauthorized(new
                    {
                        message =
                            "Your account is not approved. Please reach out to the Super Admin for approval."
                    });
                }

                // ============================================================
                // CHECK ACTIVATION
                // ============================================================

                if (!isSuperAdmin &&
                    string.Equals(
                        userDetails.Activation?.Trim(),
                        "Inactive",
                        StringComparison.OrdinalIgnoreCase))
                {
                    loginHistory.UserID = userDetails.UserId;
                    loginHistory.LoginStatus = "Inactive account";

                    _context.LoginHistory.Add(loginHistory);
                    await _context.SaveChangesAsync();

                    return Unauthorized(new
                    {
                        message =
                            "Please activate your account before login."
                    });
                }

                // ============================================================
                // CHECK ROLE MAPPING
                // ============================================================

                if (!isSuperAdmin &&
                    userDetails.RoleId <= 0)
                {
                    loginHistory.UserID = userDetails.UserId;
                    loginHistory.LoginStatus = "Role not assigned";

                    _context.LoginHistory.Add(loginHistory);
                    await _context.SaveChangesAsync();

                    return Unauthorized(new
                    {
                        message =
                            "Your account is approved, but no role has been assigned. Please reach out to the Super Admin for role mapping."
                    });
                }

                // ============================================================
                // CHECK COMPANY MAPPING
                // ============================================================

                if (!isSuperAdmin)
                {
                    var hasCompanyMapping =
                        await _context.UserMappedCompanies
                            .AnyAsync(x =>
                                x.UserId == userDetails.UserId);

                    if (!hasCompanyMapping)
                    {
                        loginHistory.UserID = userDetails.UserId;
                        loginHistory.LoginStatus =
                            "Company mapping not assigned";

                        _context.LoginHistory.Add(loginHistory);
                        await _context.SaveChangesAsync();

                        return Unauthorized(new
                        {
                            message =
                                "Your account is approved, but no company mapping has been assigned. Please reach out to the Super Admin for company mapping."
                        });
                    }
                }

                // ============================================================
                // FETCH SCREEN LIST
                // ============================================================

                var screenList =
                    await _context.ScreenMappingWithRoles
                        .Where(x =>
                            x.RoleId == userDetails.RoleId &&
                            x.Status == "Y")
                        .Select(x => new
                        {
                            x.ScreenId,
                            x.ScreenName,
                            x.Status
                        })
                        .ToListAsync();

                // ============================================================
                // CHECK SCREEN MAPPING
                // ============================================================

                if (!isSuperAdmin &&
                    !screenList.Any())
                {
                    loginHistory.UserID = userDetails.UserId;
                    loginHistory.LoginStatus = "No screen mapping";

                    _context.LoginHistory.Add(loginHistory);
                    await _context.SaveChangesAsync();

                    return Unauthorized(new
                    {
                        message =
                            "Your role has no screen mapping assigned. Please reach out to the Super Admin for screen mapping."
                    });
                }

                // ============================================================
                // SUCCESSFUL LOGIN
                // ============================================================

                loginHistory.UserID = userDetails.UserId;
                loginHistory.LoginStatus = "Success";

                _context.LoginHistory.Add(loginHistory);
                await _context.SaveChangesAsync();

                // ============================================================
                // GENERATE JWT TOKEN
                // ============================================================

                var token = GenerateJwtToken(userDetails);

                // ============================================================
                // RESPONSE
                // ============================================================

                return Ok(new
                {
                    message = "Login successful",

                    userId = userDetails.UserId,

                    companyId = userDetails.CompanyId,

                    name = userDetails.Name,

                    userName = userDetails.UserName,

                    email = userDetails.Email,

                    role = userDetails.RoleName,

                    screenList = screenList,

                    token = token,

                    loginTime = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                // TODO: Log exception

                return StatusCode(500, new
                {
                    message = "Server error. Please try again later."
                });
            }
        }

        [HttpGet("TestForgotPassword")]
        public IActionResult TestForgotPassword()
        {
            var user = new Users
            {
                Id = 23,
                UserName = "Lakshmi",
                Email = "lakshmikaku2708@gmail.com"
            };

            string storedHash =
                "AQAAAAIAAYagAAAAEOTHMcibl3M4Hwj8wBSNlIinIvgDOrsSteHXTPQzyXyz0O27tslSWaTCpy+MnH4y1g==";

            string passcode = "619424";

            var result = _passwordHasher.VerifyHashedPassword(
                user,
                storedHash,
                passcode
            );

            return Ok(new
            {
                result = result.ToString(),
                success =
                    result == PasswordVerificationResult.Success ||
                    result == PasswordVerificationResult.SuccessRehashNeeded
            });
        }
        [HttpPost("login_old")]
        public async Task<IActionResult> Login_old([FromBody] LoginModel model)
        {
            try
            {
                // Get client IP
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();


                // Check user credentials
                var userDetails = await (
    from u in _context.Users

    join r in _context.Roles
        on u.RoleId equals r.Id into roleGroup

    from r in roleGroup.DefaultIfEmpty()

    where
        (
            u.UserName.Trim() == model.UserName.Trim()
            || u.Email.Trim() == model.UserName.Trim()
        )
        && u.Password.Trim() == model.Password.Trim()

    select new
    {
        UserId = u.Id,
        u.UserName,
        u.Name,
        u.Email,
        u.CompanyId,
        u.Activation,

        RoleName = r != null
            ? r.RoleName
            : null,

        RoleId = r != null
            ? r.Id
            : 0,

        u.Status,
        u.Password
    }
).FirstOrDefaultAsync();

                // Prepare login history record
                var loginHistory = new LoginHistory
                {
                    UserName = model.UserName,
                    IPaddressOfSystem = ipAddress,
                    LoginDateTime = DateTime.Now,
                    IsLocked = "No"
                };

                if (userDetails == null)
                {
                    loginHistory.LoginStatus = "Invalid Credentials";
                    _context.LoginHistory.Add(loginHistory);
                    await _context.SaveChangesAsync();

                    return Unauthorized("Invalid Username or Password");
                }

                var isSuperAdmin = string.Equals(userDetails.RoleName?.Trim(), "SuperAdmin", StringComparison.OrdinalIgnoreCase) ||
                                   string.Equals(userDetails.RoleName?.Trim(), "Super Admin", StringComparison.OrdinalIgnoreCase);

                if (!isSuperAdmin && userDetails.Status == "Declined")
                {
                    loginHistory.LoginStatus = "Account is  Declined";
                    _context.LoginHistory.Add(loginHistory);
                    await _context.SaveChangesAsync();

                    return Unauthorized(
                        "Your account is Declined."
                    );
                }

                if (!isSuperAdmin && userDetails.Status != "Approved")
                {
                    loginHistory.LoginStatus = "Account is not approved";
                    _context.LoginHistory.Add(loginHistory);
                    await _context.SaveChangesAsync();

                    return Unauthorized(
                        "Your account is not approved. Please reach out to the Super Admin for approval."
                    );
                }
                if (!isSuperAdmin && userDetails.Activation == "Inactive")
                {
                    loginHistory.UserID = userDetails.UserId;
                    loginHistory.LoginStatus = "Inactive account";
                    _context.LoginHistory.Add(loginHistory);
                    await _context.SaveChangesAsync();

                    return Unauthorized("Please activate your account before login.");
                }

                // CHECK ROLE MAPPING
                if (!isSuperAdmin && userDetails.RoleId <= 0)
                {
                    loginHistory.LoginStatus = "Role not assigned";
                    _context.LoginHistory.Add(loginHistory);
                    await _context.SaveChangesAsync();

                    return Unauthorized(
                        "Your account is approved, but no role has been assigned. Please reach out to the Super Admin for role mapping."
                    );
                }

                // CHECK COMPANY MAPPING
                if (!isSuperAdmin)
                {
                    var hasCompanyMapping = await _context.UserMappedCompanies
                        .AnyAsync(x => x.UserId == userDetails.UserId);

                    if (!hasCompanyMapping)
                    {
                        loginHistory.LoginStatus = "Company mapping not assigned";
                        _context.LoginHistory.Add(loginHistory);
                        await _context.SaveChangesAsync();

                        return Unauthorized(
                            "Your account is approved, but no company mapping has been assigned. Please reach out to the Super Admin for company mapping."
                        );
                    }
                }


                // Successful login
                loginHistory.UserID = userDetails.UserId;
                loginHistory.LoginStatus = "Success";
                _context.LoginHistory.Add(loginHistory);
                await _context.SaveChangesAsync();

                // Fetch screen list based on RoleId
                var screenList = await _context.ScreenMappingWithRoles
                                        .Where(x => x.RoleId == userDetails.RoleId && x.Status == "Y")
                                        .Select(x => new
                                        {
                                            x.ScreenId,
                                            x.ScreenName,
                                            x.Status
                                        })
                                        .ToListAsync();
                if (!isSuperAdmin && !screenList.Any())
                {

                    return Unauthorized(
                        "Your role has no screen mapping assigned. Please reach out to the Super Admin for screen mapping."
                    );
                }

                var token = GenerateJwtToken(userDetails);



                return Ok(new
                {
                    message = "Login successful",
                    userId = userDetails.UserId,
                    password = userDetails.Password,
                    companyId = userDetails.CompanyId,
                    name = userDetails.Name,
                    userName = userDetails.UserName,
                    email = userDetails.Email,
                    role = userDetails.RoleName,
                    screenList = screenList,
                    token = token,
                    loginTime = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                // Log exception (file / db / logger)
                return StatusCode(500, "Server error. Please try again later.");
            }
        }

         private string GenerateJwtToken(dynamic userDetails)
        {
            var jwtSettings = _configuration.GetSection("Jwt");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
   {
    new Claim(JwtRegisteredClaimNames.Sub, userDetails.UserName?.ToString() ?? ""),
    new Claim("userId", userDetails.UserId.ToString()),
    new Claim("role", userDetails.RoleName?.ToString() ?? ""),
    new Claim("email", userDetails.Email?.ToString() ?? ""),
    new Claim("companyId", userDetails.CompanyId?.ToString() ?? ""),
    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
};

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(Convert.ToDouble(jwtSettings["ExpiresInMinutes"])),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        [Authorize]
        [HttpGet("check-session")]
        public IActionResult CheckSession()
        {
            // Extract user info from JWT token claims
            var userId = User.Claims.FirstOrDefault(c => c.Type == "userId")?.Value;
            var email = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var role = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
            var companyId = User.Claims.FirstOrDefault(c => c.Type == "companyId")?.Value;
            var name = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;


            if (string.IsNullOrEmpty(email))
                return Unauthorized("Token invalid or expired.");

            return Ok(new
            {
                message = "Token valid",
                email,
                userId,
                role
            });
        }


        [HttpPost("sendNotificationforForgetPassword")]
        public async Task<IActionResult> sendNotificationforForgetPassword([FromQuery] string email)
        {
            try
            {
                // Find user
                var user = await _context.Users
                    .FirstOrDefaultAsync(x => x.Email == email);

                if (user == null)
                {
                    return NotFound(new
                    {
                        message = "User not found."
                    });
                }
                

                // Generate 6-digit passcode
                var passcode = RandomNumberGenerator
                    .GetInt32(100000, 1000000)
                    .ToString();

                // ============================================================
                // HASH THE PASSCODE BEFORE SAVING
                // ============================================================
                var encryptedPassword = _passwordEncryptionService.Encrypt(passcode);

                user.Password = _passwordHasher.HashPassword(
                    user,
                    passcode
                );
                user.EncryptedPassword = encryptedPassword;
                user.ModifiedDate = DateTime.Now;
                user.ModifiedBy = "Forgot Password";

                _context.Users.Update(user);
                await _context.SaveChangesAsync();

                // ============================================================
                // SEND PLAIN PASSCODE TO EMAIL
                // ============================================================

                var emailMessage = new EmailMessage
                {
                    ToEmail = email,
                    Subject = "Password Reset Code",
                    Body = $@"
                <p>Dear User,</p>

                <p>Your password reset code is:</p>

                <h2>{passcode}</h2>

                <p>Please use this code to log in and reset your password.</p>

                <p>For security reasons, please do not share this code with anyone.</p>
            ",
                    IsBodyHtml = true
                };

                await _emailService.SendEmailAsync(emailMessage);

                return Ok(new
                {
                    message = "Password reset code sent to email successfully."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Unable to send password reset code.",
                    error = ex.Message
                });
            }
        }
        [HttpPost("postsavechangepassword")]
        public async Task<IActionResult> postsavechangepassword([FromQuery] int UserId,[FromQuery] string newpassword,[FromQuery] string oldpassword)
        {
            try
            {
                // ============================================================
                // FIND USER
                // ============================================================

                var user = await _context.Users
                    .FirstOrDefaultAsync(x => x.Id == UserId);

                if (user == null)
                {
                    return NotFound(new
                    {
                        message = "User not found."
                    });
                }


                // ============================================================
                // CHECK OLD PASSWORD
                // ============================================================

                var oldPasswordResult =
                    _passwordHasher.VerifyHashedPassword(
                        user,
                        user.Password,
                        oldpassword
                    );

                if (oldPasswordResult == PasswordVerificationResult.Failed)
                {
                    return BadRequest(new
                    {
                        message = "Old password is not matching."
                    });
                }


                // ============================================================
                // CHECK OLD PASSWORD AND NEW PASSWORD
                // ============================================================

                var samePasswordResult =
                    _passwordHasher.VerifyHashedPassword(
                        user,
                        user.Password,
                        newpassword
                    );

                if (samePasswordResult == PasswordVerificationResult.Success)
                {
                    return BadRequest(new
                    {
                        message = "Old password and new passwords are same."
                    });
                }


                // ============================================================
                // HASH NEW PASSWORD
                // ============================================================

                user.Password =
                    _passwordHasher.HashPassword(
                        user,
                        newpassword
                    );
                var encryptedPassword =
               _passwordEncryptionService.Encrypt(newpassword);

                user.EncryptedPassword = encryptedPassword;
                user.ModifiedDate = DateTime.Now;

                _context.Users.Update(user);

                await _context.SaveChangesAsync();


                // ============================================================
                // SUCCESS
                // ============================================================

                return Ok(new
                {
                    message = "Password changed successfully."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Server error. Please try again later."
                });
            }
        }
        [HttpGet("getUserDetailswithEmail")]
        public async Task<IActionResult> getUserDetailswithEmail([FromQuery] string email)
        {
            var user = await _context.Users.FirstOrDefaultAsync(x => x.Email == email);

            if (user == null)
            {
                return NotFound(new { message = "User not found." });
            }

            return Ok(new { list = user });
        }
        [HttpPost("EditProfilewithEmail")]

        public async Task<IActionResult> EditProfilewithEmail([FromBody] RegistrationModelDTo model)
        {

            //string? loggedEmail = HttpContext.Session.GetString("LoggedEmail");


            var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == model.UserId);
            if (user == null)
            {
                return NotFound(new { message = "No User found." });
            }

            var emailExists = await _context.Users.AnyAsync(x => x.Email == model.email && x.Id != model.UserId);
            if (emailExists)
            {
                return NotFound(new { message = "Email already exists" });
            }
            user.Title = model.title;
            user.Name = model.name;
            user.Email = model.email;
            user.UserName = model.username;
            user.Password = model.password;
            user.SurName = model.surname;
            user.Mobile = model.mobile;
            user.Address = model.address;
            //user.CompanyName = model.company;
            user.Gender = model.gender;
            //user.Status = model.Status;

            user.ModifiedDate = DateTime.Now;
            //user.ModifiedBy = loggedEmail;
            user.ModifiedBy = "Admin";



            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            //await _auditService.AddUserAuditAsync(user, "User Profile Updated Successfully");


            return Ok(new { message = "Profile updated successfully." });
        }

        [HttpGet("getActiveURL")]
        [AllowAnonymous]

        public async Task<IActionResult> getActiveURL()
        {
            var url = await _context.NotificationsControlTable.Where(x => x.Status == "Y").Select(x => x.NotificationType).ToListAsync();

            if (url == null)
            {
                return NotFound(new { message = "URL not found." });
            }

            return Ok(new { urlname = url });
        }



        [HttpGet("Gethelpinfowithscreen")]
        [AllowAnonymous]

        public async Task<IActionResult> Gethelpinfowithscreen(String Screenname)
        {

            var helpinfo = await _context.HelpControlTable.Where(x => x.ScreenName == Screenname && x.Status == "Y").Select(x => x.Description).FirstOrDefaultAsync();

            return Ok(new { list = helpinfo });
        }

    }
}
