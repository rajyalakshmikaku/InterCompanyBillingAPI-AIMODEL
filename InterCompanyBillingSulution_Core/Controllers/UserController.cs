using InterCompanyBillingSulution.DataAcess.Data;
using InterCompanyBillingSulution.DataAcess.Migrations;
using InterCompanyBillingSulution.Models;
using InterCompanyBillingSulution.Models.DTO;
using InterCompanyBillingSulution_Core;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Runtime.Serialization;
using System.Security.Claims;
using System.Security.Cryptography;
using static InterCompanyBillingSulution_Core.Controllers.UserController;

namespace InterCompanyBillingSulution_Core.Controllers
{
 
    public class UserController : Controller
    {
        private readonly InterCompanyBillingDbContext _context;
        private readonly PasswordHasher<Users> _passwordHasher = new PasswordHasher<Users>();

        private readonly IEmailService _emailService;
        private readonly AuditService _auditService;

        private readonly PasswordEncryptionService _passwordEncryptionService;

        public UserController(InterCompanyBillingDbContext context, IEmailService emailService, AuditService auditService, PasswordEncryptionService passwordEncryptionService)
        {
            _context = context;
            _emailService = emailService;
            _auditService = auditService;
            _passwordEncryptionService = passwordEncryptionService;
        }
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet("getCustomersList")]
        public async Task<IActionResult> GetCustomersList()
        {
            var CustomersList = await (
                from u in _context.Users
                join r in _context.Roles on u.RoleId equals r.Id
                join c in _context.Companies on u.CompanyId equals c.Id
                where u.Status == "Y" && r.RoleName == "Customer"
                select new
                {
                    u.Id,
                    u.UserName,
                    u.Email,
                    u.Mobile,
                    u.Address,
                    u.CustomerAccountNo,
                    r.RoleName,
                    c.CompanyName

                }).ToListAsync();


            return Ok(new { list = CustomersList });
        }
        [HttpGet("getRolesList")]
        public async Task<IActionResult> GetRolesList()
        {

            var Roleslist = await _context.Roles.Where(x => x.Status == "Active").ToListAsync();

            return Ok(new { list = Roleslist });
        }

        [HttpGet("getRolesListforFilter")]
        public async Task<IActionResult> getRolesListforFilter()
        {

            var Roleslist = await _context.Roles.ToListAsync();

            return Ok(new { list = Roleslist });
        }

        [HttpGet("getScreensList")]
        public async Task<IActionResult> GetScreensList()
        {

            var Screenslist = await _context.Screens.Where(x => x.Status == "Y").ToListAsync();

            return Ok(new { list = Screenslist });
        }
        [HttpGet("getRolewiseScreensList")]
        public async Task<IActionResult> GetRolewiseScreensList()
        {

            var rolescreenList = await (
                     from u in _context.ScreenMappingWithRoles
                     join r in _context.Roles on u.RoleId equals r.Id
                     where u.Status == "Y"
                     select new
                     {
                         u.ScreenId,
                         u.ScreenName,
                         r.RoleName
                     }).ToListAsync();

            return Ok(new { list = rolescreenList });
        }


        [HttpGet("getReceivingList")]
        public async Task<IActionResult> GetReceivingList()
        {
            var loggedRole =
                User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;

            var userIdClaim =
                User.Claims.FirstOrDefault(c => c.Type == "userId")?.Value;

            if (!int.TryParse(userIdClaim, out var loggedUserId))
            {
                return Unauthorized("UserId claim is missing or invalid.");
            }

            bool isSuperAdmin =
                loggedRole == "Super Admin" ||
                loggedRole == "SuperAdmin";

            IQueryable<Companies> companiesQuery =
                _context.Companies.Where(x => x.Status == "Y");

            if (!isSuperAdmin)
            {
                var mappedCompanyIds = _context.UserMappedCompanies
                    .Where(x => x.UserId == loggedUserId)
                    .Select(x => x.CompanyId);

                companiesQuery = companiesQuery
                    .Where(x => mappedCompanyIds.Contains(x.Id));
            }

            var entitieslist = await companiesQuery
                .OrderBy(x => x.CompanyName)
                .ToListAsync();

            return Ok(new
            {
                list = entitieslist
            });
        }


        [HttpGet("getCompaniesListwithdeactivatestatus")]
        public async Task<IActionResult> getCompaniesListwithdeactivatestatus()
        {
            var loggedRole = User.Claims
                .FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;

            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "userId")?.Value;
            var loggedEmail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

            int loggedUserId = 0;
            if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out var id))
            {
                loggedUserId = id;
            }


            bool isSuperAdmin =
                loggedRole == "Super Admin" ||
                loggedRole == "SuperAdmin";

            IQueryable<Companies> companiesQuery =
                _context.Companies.Where(x => x.Status == "Active");

            if (!isSuperAdmin)
            {
                var mappedCompanyIds = _context.UserMappedCompanies
                    .Where(x => x.UserId == loggedUserId)
                    .Select(x => x.CompanyId);

                companiesQuery = companiesQuery
                    .Where(x => mappedCompanyIds.Contains(x.Id));
            }

            var entitieslist = await companiesQuery.ToListAsync();

            return Ok(new { list = entitieslist });
        }


        [HttpGet("getCompaniesListforfilter")]
        public async Task<IActionResult> getCompaniesListforfilter()
        {
            var loggedRole =
                User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;

            var userIdClaim =
                User.Claims.FirstOrDefault(c => c.Type == "userId")?.Value;

            if (!int.TryParse(userIdClaim, out var loggedUserId))
            {
                return Unauthorized("UserId claim is missing or invalid.");
            }

            bool isSuperAdmin = loggedRole?.Trim().ToLower() == "super admin" || loggedRole?.Trim().ToLower() == "superadmin" || loggedRole?.Trim().ToLower() == "admin";


            IQueryable<Companies> companiesQuery = _context.Companies;

            if (!isSuperAdmin)
            {
                var mappedCompanyIds = _context.UserMappedCompanies
                    .Where(x => x.UserId == loggedUserId)
                    .Select(x => x.CompanyId);

                companiesQuery = companiesQuery
                    .Where(x => mappedCompanyIds.Contains(x.Id));
            }

            var entitieslist = await companiesQuery
                .OrderBy(x => x.CompanyName)
                .ToListAsync();

            return Ok(new
            {
                list = entitieslist
            });
        }
        [HttpPost("saveregistration")]
        public async Task<IActionResult> saveregistration([FromBody] RegistrationModelDTo model)
        {

            var emailExists = _context.Users.FirstOrDefault(x => x.Email == model.email);
            var usernameExists = await _context.Users.FirstOrDefaultAsync(x => x.UserName.Trim().ToLower() == model.username.Trim().ToLower());

            // ============================================================
            // USERNAME ALREADY EXISTS
            // ============================================================
            var encryptedPassword =
_passwordEncryptionService.Encrypt(model.password);
            if (usernameExists != null)
            {
                // If the username belongs to the same email,
                // let the email logic handle the existing account.
                if (!string.Equals(
                        usernameExists.Email?.Trim(),
                        model.email?.Trim(),
                        StringComparison.OrdinalIgnoreCase))
                {
                    return Conflict(new
                    {
                        message = "Username already exists. Please choose a different username."
                    });
                }
            }

            if (emailExists != null)
            {

                if (emailExists.Status == "Approved" && emailExists.Activation == "Active")
                {
                    return Conflict(new
                    {
                        message = "Email already exists and is currently active."
                    });
                }
                else if (emailExists.Status == "Approved" && emailExists.Activation == "Inactive")
                {
                    return Conflict(new
                    {
                        message = "This email is already registered and approved, but your account is currently inactive. Please activate your account to continue."
                    });
                }
                else if (emailExists.Status == "Created" && emailExists.Activation == "Inactive")
                {
                    return Conflict(new
                    {
                        message = "Email already exists but is currently inactive. Please contact the Super Admin for approval and an activation link."
                    });
                }

                if (emailExists.Status == "Declined")
                {
                    var mappedCompanies = await _context.UserMappedCompanies.Where(x => x.UserId == emailExists.Id).ToListAsync();

                    if (mappedCompanies.Any())
                    {
                        _context.UserMappedCompanies.RemoveRange(mappedCompanies);
                    }
            
                    // Update the existing declined account
                    emailExists.Title = model.title;
                    emailExists.Name = model.name;
                    emailExists.UserName = model.username;
                    emailExists.Password = _passwordHasher.HashPassword(null, model.password);
                    emailExists.EncryptedPassword = encryptedPassword;
                    emailExists.SurName = model.surname;
                    emailExists.Mobile = model.mobile;
                    emailExists.Address = model.address;
                    emailExists.RoleId = 0;
                    emailExists.CompanyId = model.company;
                    emailExists.Gender = model.gender;
                    emailExists.Status = "Created";
                    emailExists.Activation = "Inactive";

                    emailExists.ModifiedDate = DateTime.Now;
                    emailExists.ModifiedBy = model.email;

                    // Clear previous decline reason
                    emailExists.Reason = null;

                    await _context.SaveChangesAsync();

                    await _auditService.AddUserAuditAsync(
                        emailExists,
                        "User Re-Registration Submitted Successfully"
                    );

                    return Ok(new
                    {
                        message = "Your registration has been resubmitted successfully. Please contact the Super Admin for approval."
                    });
                }


            }


            var user = new Users
            {
                Title = model.title,
                Name = model.name,
                Email = model.email,
                UserName = model.username,
                Password = _passwordHasher.HashPassword(null, model.password),
                EncryptedPassword = encryptedPassword,

                SurName = model.surname,
                Mobile = model.mobile,
                Address = model.address,
                RoleId = model.role,
                CompanyId = model.company,
                Status = "Created",
                // Status = "Y",
                Activation = "Inactive",
                CreatedDate = DateTime.Now,
                ModifiedDate = DateTime.Now,
                CreatedBy = model.email,
                ModifiedBy = model.email,
                CustomerAccountNo = "12345",
                Gender = model.gender,

            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            await _auditService.AddUserAuditAsync(user, "User Registration Created Successfully");


            var role = _context.Roles.Where(x => x.Id == user.RoleId).Select(x => x.RoleName).FirstOrDefault();

            //var activationUrl = $"{model.activationlink}?email={model.email}";


            //var emailMessage = new EmailMessage
            //{
            //    ToEmail = model.email,
            //    Subject = "Registration Confirmation – Inter-Company Billing System",
            //    Body = $@"
            //         <div style='font-family:Arial, sans-serif; color:#000;'>

            //         <p>Dear {model.username},</p>

            //         <p>We're pleased to inform you that you have been successfully registered to the 
            //         <strong>Inter-Company Billing System</strong>.</p>

            //         <p><strong>Registration Details:</strong></p>
            //         <ul>
            //             <li><strong>Username:</strong> {model.username}</li>
            //             <li><strong>Role Assigned:</strong> {role}</li>
            //             <li><strong>System Access:</strong> In active</li>
            //             Please click <a href='{activationUrl}'>here</a> to actviate your account.</p>
            //         </ul>

            //         <p>You can now log in and begin using the system according to your assigned role. </p>

            //         <br/>
            //         <p>Kind Regards,<br/>
            //         ITSA Support Team<br/>
            //         <a href='mailto:support@itsa.co.za'>support@itsa.co.za</a></p>
            //         </div>",
            //    IsBodyHtml = true
            //};



            //await _emailService.SendEmailAsync(emailMessage);




            return Ok(new
            {
                message = "User registered successfully. Please contact the Super Admin for approval and mapping details."
            });
        }

        [HttpPost("saveactivationstatus")]
        public async Task<IActionResult> SaveActivationStatus(string email)
        {
            var user = await _context.Users.FirstOrDefaultAsync(x => x.Email == email);

            if (user == null)
            {
                return NotFound(new { message = "User not found." });
            }

            if (user.Activation == "Active")
            {
                return Ok(new { message = "Your account is already activated." });
            }

            // Update activation status
            user.Activation = "Active";
            user.ModifiedBy = email;
            user.ModifiedDate = DateTime.Now;

            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            //  await _auditService.AddUserAuditAsync(user, "User account has been activated successfully.");


            return Ok(new { message = "Your account has been activated successfully." });
        }


        [HttpGet("checkactivationstatus")]
        public async Task<IActionResult> CheckActivationStatus(string email)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(x => x.Email == email);

            if (user == null)
            {
                return NotFound(new
                {
                    message = "User not found."
                });
            }

            return Ok(new
            {
                id = user.Id,
                activation = user.Activation,
                message = user.Activation == "Active"
                    ? "Your account is already activated."
                    : "Your account is currently inactive. Please activate your account."
            });
        }


        [HttpPost("savescreenmappingdata")]
        public async Task<IActionResult> SaveScreenMappingData([FromBody] ScreenMappingDto dto)
        {
            string? loggedEmail = HttpContext.Session.GetString("LoggedEmail");

            if (dto == null || dto.ScreenIds == null || !dto.ScreenIds.Any())
            {
                return BadRequest("Invalid input data.");
            }


            var oldMappings = _context.ScreenMappingWithRoles.Where(m => m.RoleId == dto.RoleId && m.Status == "Y");

            _context.ScreenMappingWithRoles.RemoveRange(oldMappings);


            var screens = await _context.Screens
                .Where(s => dto.ScreenIds.Contains(s.Id))
                .ToListAsync();

            var newMappings = screens.Select(screen => new ScreenMappingWithRole
            {
                RoleId = dto.RoleId,
                ScreenId = screen.Id,
                ScreenName = screen.ScreenName,
                CreatedDate = DateTime.Now,
                CreatedBy = "Super Admin",
                ModifiedDate = DateTime.Now,
                ModifiedBy = "Super Admin",
                Status = "Y",
            });

            await _context.ScreenMappingWithRoles.AddRangeAsync(newMappings);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Screens are mapped successfully." });
        }

        [HttpGet("getuserslist")]
        public async Task<IActionResult> GetUsersList()
        {



            var userlist = await (
                from u in _context.Users
                join r in _context.Roles on u.RoleId equals r.Id
                join c in _context.Companies on u.CompanyId equals c.Id
                select new
                {
                    u.Id,
                    u.Name,
                    u.SurName,
                    u.Mobile,
                    u.Address,
                    u.Title,
                    u.Gender,
                    u.Password,
                    u.RoleId,
                    u.UserName,
                    u.Email,
                    u.CustomerAccountNo,
                    c.CompanyName,
                    RoleName = r.RoleName
                }).ToListAsync();

            return Ok(new { list = userlist });
        }

        [HttpGet("getUsersListforApproval")]
        public async Task<IActionResult> GetUsersListforApproval()
        {
            var userlist = await _context.Users.OrderByDescending(u => u.Id)
                .Select(u => new
                {
                    u.Id,
                    u.Name,
                    u.SurName,
                    u.Mobile,
                    u.Address,
                    u.Title,
                    u.Gender,
                    u.RoleId,
                    u.UserName,
                    u.Email,
                    u.CustomerAccountNo,
                    u.Status,
                    u.Reason,
                    u.Activation,

                    // Company mappings
                    CompanyMappings = _context.UserMappedCompanies
                        .Where(x => x.UserId == u.Id)
                        .Select(x => new
                        {
                            x.CompanyId,
                            x.CompanyName
                        })
                        .ToList()
                })
                .ToListAsync();

            return Ok(new { list = userlist });
        }
        [HttpPost("approveUser/{id}")]
        public async Task<IActionResult> ApproveUser(int id, [FromQuery] string activationlink)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);

                if (user == null)
                {
                    return NotFound(new
                    {
                        message = "User not found."
                    });
                }

                // Approve user
                user.Status = "Approved";
                user.Activation = "Inactive";
                user.ModifiedDate = DateTime.Now;
                user.ModifiedBy = user.Email;

                await _context.SaveChangesAsync();

                // Get role
                var role = await _context.Roles
                    .Where(x => x.Id == user.RoleId)
                    .Select(x => x.RoleName)
                    .FirstOrDefaultAsync();

                // Activation URL
                var activationUrl =
                    $"{activationlink}?email={Uri.EscapeDataString(user.Email)}";

                // Email
                var emailMessage = new EmailMessage
                {
                    ToEmail = user.Email,
                    Subject = "Account Approved – Inter-Company Billing System",

                    Body = UserEmailTemplate(
                        user.UserName,
                        user.Password,
                        role ?? "Not Assigned",
                        activationUrl,
                        null,
                        "Approve",
                        null
                    ),

                    IsBodyHtml = true
                };

                await _emailService.SendEmailAsync(emailMessage);

                // Audit
                await _auditService.AddUserAuditAsync(
                    user,
                    "User Account Approved Successfully"
                );

                return Ok(new
                {
                    message = "User approved successfully. Activation email sent.",
                    status = user.Status
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Error approving user.",
                    error = ex.Message
                });
            }
        }

        [HttpPost("declineUser/{id}")]
        public async Task<IActionResult> DeclineUser(int id, [FromBody] DeclineUserRequest request)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);

                if (user == null)
                {
                    return NotFound(new
                    {
                        message = "User not found."
                    });
                }
                string reason = request.Reason;
                string registrationUrl = request.RegistrationUrl;

                // Decline user
                user.Status = "Declined";
                user.Activation = "Inactive";
                user.ModifiedDate = DateTime.Now;
                user.ModifiedBy = user.Email;
                user.Reason = reason;

                await _context.SaveChangesAsync();

                // Get role
                var role = await _context.Roles
                    .Where(x => x.Id == user.RoleId)
                    .Select(x => x.RoleName)
                    .FirstOrDefaultAsync();

               

                // Email
                var emailMessage = new EmailMessage
                {
                    ToEmail = user.Email,
                    Subject = "Account Application Declined – Inter-Company Billing System",

                    Body = UserEmailTemplate(
                        user.UserName,
                        null,
                        role ?? "Not Assigned",
                        registrationUrl,
                        null,
                        "Decline",
                       user.Reason
                       

                    ),

                    IsBodyHtml = true
                };

                await _emailService.SendEmailAsync(emailMessage);

                // Audit
                await _auditService.AddUserAuditAsync(
                    user,
                    "User Account Declined Successfully"
                );

                return Ok(new
                {
                    message = "User declined successfully.",
                    status = user.Status
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Error declining user.",
                    error = ex.Message
                });
            }
        }

        // ============================================================
        // MAP USER ROLE & COMPANIES
        // ============================================================

        [HttpPost("mapUserRoleCompanies/{id}")]
        public async Task<IActionResult> MapUserRoleCompanies(
            int id,
            [FromBody] MapUserRoleCompaniesRequest request)
        {
            using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                // ========================================================
                // GET USER
                // ========================================================

                var user = await _context.Users.FindAsync(id);

                if (user == null)
                {
                    return NotFound(new
                    {
                        message = "User not found."
                    });
                }
                string plainPassword = null;

                if (!string.IsNullOrWhiteSpace(user.EncryptedPassword))
                {
                    plainPassword =
                        _passwordEncryptionService.Decrypt(
                            user.EncryptedPassword
                        );
                }

                // ========================================================
                // VALIDATE ROLE
                // ========================================================

                var role = await _context.Roles
                    .FirstOrDefaultAsync(x => x.Id == request.RoleId);

                if (role == null)
                {
                    return BadRequest(new
                    {
                        message = "Invalid role."
                    });
                }


                // ========================================================
                // GET COMPANY IDS
                // ========================================================

                var companyIds = request.CompanyIds?
                    .Distinct()
                    .ToList() ?? new List<int>();


                // ========================================================
                // VALIDATE COMPANIES
                // ========================================================

                var companies = await _context.Companies
                    .Where(x => companyIds.Contains(x.Id))
                    .ToListAsync();

                if (companies.Count != companyIds.Count)
                {
                    return BadRequest(new
                    {
                        message = "One or more companies are invalid."
                    });
                }


                // ========================================================
                // UPDATE USER ROLE ONLY
                // ========================================================

                user.RoleId = request.RoleId;
                user.ModifiedDate = DateTime.Now;
                user.ModifiedBy = user.Email;


                // ========================================================
                // REMOVE OLD COMPANY MAPPINGS
                // ========================================================

                var existingMappings = await _context.UserMappedCompanies
                    .Where(x => x.UserId == user.Id)
                    .ToListAsync();

                if (existingMappings.Any())
                {
                    _context.UserMappedCompanies.RemoveRange(existingMappings);
                }


                // ========================================================
                // ADD NEW COMPANY MAPPINGS
                // ========================================================

                foreach (var company in companies)
                {
                    _context.UserMappedCompanies.Add(
                        new UserMappedCompanies
                        {
                            UserId = user.Id,
                            CompanyId = company.Id,
                            CompanyName = company.CompanyName,
                            CreatedDate = DateTime.Now,
                            ModifiedDate = null
                        });
                }


                // ========================================================
                // SAVE
                // ========================================================

                await _context.SaveChangesAsync();


                // ========================================================
                // AUDIT
                // ========================================================

                var companyNames = string.Join(
                    ", ",
                    companies.Select(x => x.CompanyName)
                );

                await _auditService.AddUserAuditAsync(
                    user,
                    $"User role and companies mapped successfully. " +
                    $"Role: {role.RoleName}. " +
                    $"Companies: {companyNames}"
                );


                // ========================================================
                // COMMIT
                // ========================================================

                await transaction.CommitAsync();

                var emailMessage = new EmailMessage
                {
                    ToEmail = user.Email,

                    Subject =
                        "Account Mapped – Inter-Company Billing System",

                   
                    Body = UserEmailTemplate(
 user.UserName,
plainPassword,
 role.RoleName,
 null,
 companyNames,
 "Map",
 null),

                    IsBodyHtml = true
                };

                await _emailService.SendEmailAsync(emailMessage);
                // ========================================================
                // RESPONSE
                // ========================================================

                return Ok(new
                {
                    message = "User role and companies mapped successfully.",

                    userId = user.Id,

                    roleId = user.RoleId,

                    roleName = role.RoleName,

                    companyIds = companyIds,

                    companyNames = companies
                        .Select(x => x.CompanyName)
                        .ToList()
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                return StatusCode(500, new
                {
                    message = "Error mapping user role and companies.",
                    error = ex.Message
                });
            }
        }


        [HttpPost("approveAndMapUser/{id}")]
        public async Task<IActionResult> ApproveAndMapUser(
            int id,
            [FromBody] ApproveAndMapUserRequest request)
        {
            using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                // ============================================================
                // GET USER
                // ============================================================

                var user = await _context.Users.FindAsync(id);

                if (user == null)
                {
                    return NotFound(new
                    {
                        message = "User not found."
                    });
                }
                string plainPassword = null;

                if (!string.IsNullOrWhiteSpace(user.EncryptedPassword))
                {
                    plainPassword =
                        _passwordEncryptionService.Decrypt(
                            user.EncryptedPassword
                        );
                }

                // ============================================================
                // VALIDATE ROLE
                // ============================================================

                var role = await _context.Roles
                    .FirstOrDefaultAsync(x => x.Id == request.RoleId);

                if (role == null)
                {
                    return BadRequest(new
                    {
                        message = "Invalid role."
                    });
                }


                // ============================================================
                // VALIDATE COMPANIES
                // ============================================================

                var companyIds = request.CompanyIds?
                    .Distinct()
                    .ToList() ?? new List<int>();

                var companies = await _context.Companies
                    .Where(x => companyIds.Contains(x.Id))
                    .ToListAsync();

                if (companies.Count != companyIds.Count)
                {
                    return BadRequest(new
                    {
                        message = "One or more companies are invalid."
                    });
                }


                // ============================================================
                // UPDATE USER ROLE
                // ============================================================

                user.RoleId = request.RoleId;

                user.Status = "Approved";
                user.Activation = "Inactive";
                user.ModifiedDate = DateTime.Now;
                user.ModifiedBy = user.Email;


                // ============================================================
                // REMOVE EXISTING COMPANY MAPPINGS
                // ============================================================

                var existingMappings = await _context.UserMappedCompanies
                    .Where(x => x.UserId == user.Id)
                    .ToListAsync();

                if (existingMappings.Any())
                {
                    _context.UserMappedCompanies.RemoveRange(existingMappings);
                }


                // ============================================================
                // ADD NEW COMPANY MAPPINGS
                // ============================================================

                foreach (var company in companies)
                {
                    var mapping = new UserMappedCompanies
                    {
                        UserId = user.Id,
                        CompanyId = company.Id,
                        CompanyName = company.CompanyName,
                        CreatedDate = DateTime.Now,
                        ModifiedDate = null
                    };

                    _context.UserMappedCompanies.Add(mapping);
                }


                // ============================================================
                // SAVE USER + COMPANY MAPPINGS
                // ============================================================

                await _context.SaveChangesAsync();


                // ============================================================
                // ACTIVATION URL
                // ============================================================

                var activationUrl =
                    $"{request.ActivationLink}?email={Uri.EscapeDataString(user.Email)}";


                // ============================================================
                // COMPANY NAMES FOR EMAIL
                // ============================================================

                var companyNames = string.Join(
                    ", ",
                    companies.Select(x => x.CompanyName)
                );


                // ============================================================
                // APPROVAL EMAIL
                // ============================================================

                var emailMessage = new EmailMessage
                {
                    ToEmail = user.Email,

                    Subject =
                        "Account Approved – Inter-Company Billing System",

                    Body = UserEmailTemplate(
                        user.UserName,
                        plainPassword,
                        role.RoleName,
                        activationUrl,
                        companyNames,
                        "ApproveAndMap",
                        null
                    ),

                    IsBodyHtml = true
                };

                await _emailService.SendEmailAsync(emailMessage);


                // ============================================================
                // AUDIT
                // ============================================================

                await _auditService.AddUserAuditAsync(
                    user,
                    $"User approved and mapped successfully. " +
                    $"Role: {role.RoleName}. " +
                    $"Companies: {companyNames}"
                );


                // ============================================================
                // COMMIT TRANSACTION
                // ============================================================

                await transaction.CommitAsync();


                // ============================================================
                // RESPONSE
                // ============================================================

                return Ok(new
                {
                    message =
                        "User approved and role/companies mapped successfully. Activation email sent.",

                    userId = user.Id,

                    status = user.Status,

                    roleId = user.RoleId,

                    roleName = role.RoleName,

                    companyIds = companyIds,

                    companyNames = companies
                        .Select(x => x.CompanyName)
                        .ToList()
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                return StatusCode(500, new
                {
                    message =
                        "Error approving and mapping user.",

                    error = ex.Message
                });
            }
        }

        public static string UserEmailTemplate(string userName,string plainPassword,string roleName = null,string activationUrl = null,string companyNames = null,string emailType = "Approve",string reason = null)
        {
            const string systemName = "Inter-Company Billing System";
            const string supportTeam = "ITSA Support Team";

            // 1. User Registration Declined
            if (emailType.Equals("Decline", StringComparison.OrdinalIgnoreCase))
            {
                return $@"
<div style='font-family:Arial, sans-serif; color:#000;'>

    <p>Dear {userName},</p>

    <p>
        Thank you for registering on the {systemName}.
    </p>

    <p>
        Unfortunately, your registration has been declined by the
        Super Administrator and your account has not been approved.
    </p>

    <p>
        <strong>Reason for Declining:</strong>
        {(!string.IsNullOrWhiteSpace(reason) ? reason : "Not Provided")}
    </p>
<p>
        <strong>Re-Registration Link:</strong>
        <a href='{activationUrl}'>Click here</a>
    </p>
     

</p>
</p>
   

    <p>
        If you require any assistance, please contact the system administrator.
    </p>

    <br/>

    <p>
        Kind regards,<br/>
        {supportTeam}
    </p>

</div>";
            }


            // 2. User Approved - Without Role and Company
            if (emailType.Equals("Approve", StringComparison.OrdinalIgnoreCase))
            {
                return $@"
<div style='font-family:Arial, sans-serif; color:#000;'>

    <p>Dear {userName},</p>

    <p>
        We are pleased to inform you that your registration on
        {systemName} has been approved.
    </p>

    <p>
        Your account has been successfully created. Please use the
        activation link and login details below to access the system.
    </p>

    <p>
        <strong>Activation Link:</strong>
        <a href='{activationUrl}'>Activate Your Account</a>
    </p>

    <p>
        <strong>Username:</strong> {userName}<br/>
       
    </p>

    <p>
        Please activate your account and log in to get started.
    </p>

    <p>
        For security purposes, we recommend changing your password
        after your first login.
    </p>

    <p>
        If you experience any issues accessing your account,
        please contact the system administrator.
    </p>

    <br/>

    <p>
        Kind regards,<br/>
        {supportTeam}
    </p>

</div>";
            }


            // 3. User Approved - With Role and Company
            if (emailType.Equals("ApproveAndMap",
                StringComparison.OrdinalIgnoreCase))
            {
                return $@"
<div style='font-family:Arial, sans-serif; color:#000;'>

    <p>Dear {userName},</p>

    <p>
        We are pleased to inform you that your registration on
        Intercompany Billing has been approved.
    </p>

    <p>
        Your account has been created and linked to the following
        organisation and user role:
    </p>

    <p>
        <strong>Company:</strong> {companyNames ?? "Not Assigned"}<br/>
        <strong>Role:</strong> {roleName ?? "Not Assigned"}
    </p>

    <p>
        Please use the details below to activate and access your account:
    </p>

    <p>
        <strong>Activation Link:</strong>
        <a href='{activationUrl}'>Activate Your Account</a>
    </p>

    <p>
        <strong>Username:</strong> {userName}<br/>
    </p>
    <p>
        <strong>Password:</strong> {plainPassword}<br/>
    </p>

    <p>
        Once you have activated your account, you can log in and access
        the features and information available according to your assigned role.
    </p>

    <p>
        For security purposes, please change your password after your
        first login.
    </p>

    <p>
        If you require any assistance, please contact the system administrator.
    </p>

    <br/>

    <p>
        Kind regards,<br/>
        {supportTeam}
    </p>

</div>";
            }


            // 4. Mapping / Account Details Updated
            if (emailType.Equals("Map", StringComparison.OrdinalIgnoreCase))
            {
                return $@"
<div style='font-family:Arial, sans-serif; color:#000;'>

    <p>Dear {userName},</p>

    <p>
        Your account on the InterCompany Billing has been successfully
        linked to the following organisation and user role:
    </p>

    <p>
        <strong>Company:</strong> {companyNames ?? "Not Assigned"}<br/>
        <strong>Role:</strong> {roleName ?? "Not Assigned"}
    </p>

    <p>
        Please use the details below to activate and access your account:
    </p>
 

    <p>
        <strong>Username:</strong> {userName}<br/>

    </p>
 
    <p>
        Once you have activated your account, you can log in and access the features and information available according to your assigned role.
    </p>
  <p>
  <p>
For security purposes, please change your password after your first login.    </p>  
  <p>
If you require any assistance, please contact the system administrator.    </p>  
    <br/>

    <p>
        Kind regards,<br/>
        {supportTeam}
    </p>

</div>";
            }


            // Default - Approved
            return $@"
<div style='font-family:Arial, sans-serif; color:#000;'>

    <p>Dear {userName},</p>

    <p>
        We are pleased to inform you that your registration on
     Intercompany Billing has been approved.
    </p>

    <p>
        Your account has been successfully created. Please use the
        activation link and login details below to access the system.
    </p>

    <p>
        <strong>Activation Link:</strong>
        <a href='{activationUrl}'>Activate Your Account</a>
    </p>

    <p>
        <strong>Username:</strong> {userName}<br/>
    </p>

    <p>
        Please activate your account and log in to get started.
    </p>

    <p>
        For security purposes, we recommend changing your password
        after your first login.
    </p>

    <p>
        If you experience any issues accessing your account,
        please contact the system administrator.
    </p>

    <br/>

    <p>
        Kind regards,<br/>
        {supportTeam}
    </p>

</div>";
        }

        public class DeclineUserRequest
        {
            public string Reason { get; set; }
            public string RegistrationUrl { get; set; }
        }

        public class MapUserRoleCompaniesRequest
        {
            public int RoleId { get; set; }
            public List<int> CompanyIds { get; set; } = new();
        }

        public class ApproveAndMapUserRequest
        {
            public string ActivationLink { get; set; }
            public int RoleId { get; set; }
            public List<int> CompanyIds { get; set; } = new();
        }

        public class EncryptionService
        {
            private readonly byte[] _key;
            private readonly byte[] _iv;

            public EncryptionService(IConfiguration config)
            {
                _key = Convert.FromBase64String(config["Encryption:Key"]);
                _iv = Convert.FromBase64String(config["Encryption:IV"]);
            }

            public string Decrypt(string cipherText)
            {
                using var aes = Aes.Create();
                aes.Key = _key;
                aes.IV = _iv;

                using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
                var buffer = Convert.FromBase64String(cipherText);

                using var ms = new MemoryStream(buffer);
                using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
                using var sr = new StreamReader(cs);
                return sr.ReadToEnd();
            }
        }

        [HttpPost("UpdateUser")]
        public async Task<IActionResult> UpdateUser([FromBody] UpdateUserDto request)
        {
            var loggedEmail = User.Claims
                .FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            using var transaction =
                await _context.Database.BeginTransactionAsync();


            try
            {
                // ========================================================
                // GET USER
                // ========================================================

                var user = await _context.Users.FindAsync(request.Id);

                if (user == null)
                {
                    return NotFound(new
                    {
                        message = "User not found."
                    });
                }
                //var emailExists = await _context.Users.AnyAsync(u => u.Email == request.Email&& u.UserName == request.UserName && u.Id != request.Id);
                var emailExists = await _context.Users
    .FirstOrDefaultAsync(x =>
        x.Email.Trim().ToLower() == request.Email.Trim().ToLower()
        && x.Id != request.Id);

                if (emailExists != null)
                {
                    return BadRequest(new
                    {
                        message = "Email already exists for another user"
                    });
                }

                var usernameExists = await _context.Users
                    .FirstOrDefaultAsync(x =>
                        x.UserName.Trim().ToLower() == request.UserName.Trim().ToLower()
                        && x.Id != request.Id);

                if (usernameExists != null)
                {
                    return BadRequest(new
                    {
                        message = "Username already exists for another user"
                    });
                }

                // ========================================================
                // GET EXISTING COMPANY MAPPINGS
                // ========================================================

                var existingCompanyIds = await _context.UserMappedCompanies
                    .Where(x => x.UserId == user.Id)
                    .Select(x => x.CompanyId)
                    .ToListAsync();

                var requestedCompanyIds = request.CompanyIds?
                    .Distinct()
                    .ToList() ?? new List<int>();

                // Compare company mappings
                bool companiesChanged =
                    !existingCompanyIds.OrderBy(x => x)
                        .SequenceEqual(requestedCompanyIds.OrderBy(x => x));


                // Check ONLY editable fields
                bool hasChanges =
                        user.Name != request.Name ||
                         user.SurName != request.SurName ||
     user.Mobile != request.Mobile ||
     user.Address != request.Address ||
     user.Title != request.Title ||
     user.Gender != request.Gender ||
     user.UserName != request.UserName ||
     user.Email != request.Email ||
     user.Activation != request.Activation ||
     user.RoleId != request.RoleId ||
     companiesChanged;

                // No changes in editable fields
                if (!hasChanges)
                    return BadRequest(new
                    {
                        message = "No changes found."
                    }); 


                // ========================================================
                // VALIDATE ROLE
                // ========================================================

                var role = await _context.Roles
                    .FirstOrDefaultAsync(x => x.Id == request.RoleId);

                if (role == null)
                {
                    return BadRequest(new
                    {
                        message = "Invalid role."
                    });
                }


                // ========================================================
                // GET COMPANY IDS
                // ========================================================
                var companyIds = requestedCompanyIds;

                // ========================================================
                // VALIDATE COMPANIES
                // ========================================================

                var companies = await _context.Companies
                    .Where(x => companyIds.Contains(x.Id))
                    .ToListAsync();

                if (companies.Count != companyIds.Count)
                {
                    return BadRequest(new
                    {
                        message = "One or more companies are invalid."
                    });
                }


                // ========================================================
                // UPDATE USER ROLE ONLY
                // ========================================================
                user.Name = request.Name;
                user.SurName = request.SurName;
                user.Mobile = request.Mobile;
                user.Address = request.Address;
                user.Title = request.Title;
                user.Gender = request.Gender;
                user.UserName = request.UserName;
                user.Email = request.Email;
                user.Activation = request.Activation;
                user.RoleId = request.RoleId;
                user.ModifiedDate = DateTime.Now;
                user.ModifiedBy = loggedEmail;


                // ========================================================
                // REMOVE OLD COMPANY MAPPINGS
                // ========================================================

                var existingMappings = await _context.UserMappedCompanies
                    .Where(x => x.UserId == user.Id)
                    .ToListAsync();

                if (existingMappings.Any())
                {
                    _context.UserMappedCompanies.RemoveRange(existingMappings);
                }


                // ========================================================
                // ADD NEW COMPANY MAPPINGS
                // ========================================================

                foreach (var company in companies)
                {
                    _context.UserMappedCompanies.Add(
                        new UserMappedCompanies
                        {
                            UserId = user.Id,
                            CompanyId = company.Id,
                            CompanyName = company.CompanyName,
                            CreatedDate = DateTime.Now,
                            ModifiedDate = null
                        });
                }


                // ========================================================
                // SAVE
                // ========================================================

                await _context.SaveChangesAsync();


                // ========================================================
                // AUDIT
                // ========================================================

                var companyNames = string.Join(
                    ", ",
                    companies.Select(x => x.CompanyName)
                );

                await _auditService.AddUserAuditAsync(
                    user,
                    $"User updated successfully. " +
                    $"Role: {role.RoleName}. " +
                    $"Companies: {companyNames}"
                );


                // ========================================================
                // COMMIT
                // ========================================================

                await transaction.CommitAsync();

               
                // ========================================================
                // RESPONSE
                // ========================================================

                return Ok(new
                {
                    message = "User Updated successfully.",

                    userId = user.Id,

                    roleId = user.RoleId,

                    roleName = role.RoleName,

                    companyIds = companyIds,

                    companyNames = companies
                        .Select(x => x.CompanyName)
                        .ToList()
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                return StatusCode(500, new
                {
                    message = "Error mapping user role and companies.",
                    error = ex.Message
                });
            }
        }

    }

}


