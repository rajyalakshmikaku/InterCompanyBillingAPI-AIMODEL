using InterCompanyBillingSulution.DataAcess.Data;
using InterCompanyBillingSulution.Models.DTO;
using InterCompanyBillingSulution.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Text.Json;
using System.Data;

namespace InterCompanyBillingSulution_Core.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class MasterController : ControllerBase
    {

        private readonly InterCompanyBillingDbContext _context;
        private readonly AuditService _auditService;

        public MasterController(InterCompanyBillingDbContext context, AuditService auditService)
        {
            _context = context;
            _auditService = auditService;

        }

              
        [HttpPost("saveMaster")]
        public async Task<IActionResult> saveMaster([FromBody] MasterSaveDTO model)
        {
            try
            {
                var userIdClaim = User.Claims
                    .FirstOrDefault(c => c.Type == "userId")?.Value;

                int loggedUserId = 0;

                if (!string.IsNullOrEmpty(userIdClaim) &&
                    int.TryParse(userIdClaim, out var id))
                {
                    loggedUserId = id;
                }

                if (model == null)
                {
                    return BadRequest(new
                    {
                        message = "Invalid request data"
                    });
                }

                if (string.IsNullOrWhiteSpace(model.EntityType))
                {
                    return BadRequest(new
                    {
                        message = "EntityType is required (MeterType, QuotationType, VAT, Status, Category)"
                    });
                }

                switch (model.EntityType.Trim().ToLower())
                {
                    // =========================
                    // METER TYPE
                    // =========================
                    case "metertype":
                        {
                            if (string.IsNullOrWhiteSpace(model.Typename))
                            {
                                return BadRequest(new
                                {
                                    message = "Meter Type is required"
                                });
                            }

                            var meterType = new MeterType
                            {
                                Type = model.Typename.Trim(),
                                Description = "",
                                Status = "Active",
                                CreatedDate = DateTime.Now,
                                ModifiedDate = DateTime.Now,
                                CreatedBy = loggedUserId.ToString(),
                                ModifiedBy = loggedUserId.ToString()
                            };

                            _context.MeterType.Add(meterType);
                            await _context.SaveChangesAsync();

                            return Ok(new
                            {
                                message = "Meter type saved successfully"
                            });
                        }

                    // =========================
                    // QUOTATION TYPE
                    // =========================
                    case "quotationtype":
                        {
                            var quotationType = new QuotationType
                            {
                                Name = model.Typename ?? "",
                                Status = "Active",
                                CreatedDate = DateTime.Now,
                                ModifiedDate = DateTime.Now,
                                CreatedBy = loggedUserId.ToString(),
                                ModifiedBy = loggedUserId.ToString()
                            };

                            _context.QuotationType.Add(quotationType);
                            await _context.SaveChangesAsync();

                            return Ok(new
                            {
                                message = "Quotation type saved successfully"
                            });
                        }

                    // =========================
                    // VAT
                    // =========================
                    case "vat":
                        {
                            var tax = new VAT
                            {
                                VATPercentage = model.TaxPercentage ?? 0,
                                SatrtDate = model.StartDate ?? DateTime.Now,
                                EndDate = model.EndDate ?? DateTime.Now,
                                Description = model.Description,
                                Status = "Active",
                                CreatedDate = DateTime.Now,
                                ModifiedDate = DateTime.Now,
                                CreatedBy = loggedUserId.ToString(),
                                ModifiedBy = loggedUserId.ToString()
                            };

                            _context.VAT.Add(tax);
                            await _context.SaveChangesAsync();

                            return Ok(new
                            {
                                message = "VAT saved successfully"
                            });
                        }

                    // =========================
                    // STATUS
                    // =========================
                    case "status":
                        {
                            var status = new Status
                            {
                                StatusName = model.StatusName,
                                Description = model.Description,
                                status = "Active",
                                CreatedDate = DateTime.Now,
                                ModifiedDate = DateTime.Now,
                                CreatedBy = loggedUserId.ToString(),
                                ModifiedBy = loggedUserId.ToString()
                            };

                            _context.Status.Add(status);
                            await _context.SaveChangesAsync();

                            return Ok(new
                            {
                                message = "Status saved successfully"
                            });
                        }

                    // =========================
                    // CATEGORY
                    // =========================
                    case "category":
                        {
                            var category = new Category
                            {
                                Name = model.CategoryName,
                                Status = "Active",
                                CreatedDate = DateTime.Now,
                                ModifiedDate = DateTime.Now,
                                CreatedBy = loggedUserId.ToString(),
                                ModifiedBy = loggedUserId.ToString()
                            };

                            _context.Categories.Add(category);
                            await _context.SaveChangesAsync();

                            return Ok(new
                            {
                                message = "Category saved successfully"
                            });
                        }

                    // =========================
                    // HELP
                    // =========================
                    case "help":
                        {
                            if (string.IsNullOrWhiteSpace(model.ScreenName))
                            {
                                return BadRequest(new
                                {
                                    message = "Screen Name is required"
                                });
                            }

                            if (string.IsNullOrWhiteSpace(model.Description))
                            {
                                return BadRequest(new
                                {
                                    message = "Description is required"
                                });
                            }

                            var help = new HelpControlTable
                            {
                                ScreenName = model.ScreenName.Trim(),
                                Description = model.Description.Trim(),
                                Status = "Y",
                                CreatedDate = DateTime.Now,
                                ModifiedDate = DateTime.Now,
                                CreatedBy = loggedUserId.ToString(),
                                ModifiedBy = loggedUserId.ToString()
                            };

                            _context.HelpControlTable.Add(help);
                            await _context.SaveChangesAsync();

                            return Ok(new
                            {
                                message = "Help saved successfully"
                            });
                        }

                    // =========================
                    // INVALID ENTITY TYPE
                    // =========================
                    default:
                        return BadRequest(new
                        {
                            message = "Invalid EntityType. Use: MeterType, QuotationType, VAT, Status, Category , Help"
                        });
                }
            }
            catch (Exception ex)
            {
                var errorMessage =
                    ex.InnerException?.InnerException?.Message ??
                    ex.InnerException?.Message ??
                    ex.Message;

                return BadRequest(new
                {
                    message = errorMessage
                });
            }
        }

        [HttpGet("getMasterList")]
        public async Task<IActionResult> GetMasterList([FromQuery] string entityType)
        {
            if (string.IsNullOrWhiteSpace(entityType))
            {
                return BadRequest(new
                {
                    message = "EntityType is required (MeterType, QuotationType, VAT, Status, Category, Help)"
                });
            }

            switch (entityType.Trim().ToLower())
            {
                case "metertype":
                    {
                        var meterTypeList = await _context.MeterType.ToListAsync();

                        return Ok(new
                        {
                            list = meterTypeList
                        });
                    }

                case "quotationtype":
                    {
                        var quotationtypeList = await _context.QuotationType.ToListAsync();

                        return Ok(new
                        {
                            list = quotationtypeList
                        });
                    }

                case "vat":
                    {
                        var taxList = await _context.VAT.ToListAsync();

                        return Ok(new
                        {
                            list = taxList
                        });
                    }

                case "status":
                    {
                        var statusList = await _context.Status.ToListAsync();

                        return Ok(new
                        {
                            list = statusList
                        });
                    }

                case "category":
                    {
                        var categoryList = await _context.Categories.ToListAsync();

                        return Ok(new
                        {
                            list = categoryList
                        });
                    }

                case "help":
                    {
                        var helpList = await _context.HelpControlTable
                            .OrderBy(x => x.ScreenName)
                            .ToListAsync();

                        return Ok(new
                        {
                            list = helpList
                        });
                    }

                default:
                    {
                        return BadRequest(new
                        {
                            message = "Invalid EntityType. Use: MeterType, QuotationType, VAT, Status, Category,Help"
                        });
                    }
            }
        }


      
        [HttpPost("updateMaster")]
        public async Task<IActionResult> UpdateMaster([FromBody] JsonElement request)
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "userId")?.Value;

            int loggedUserId = 0;

            if (!string.IsNullOrEmpty(userIdClaim) &&
                int.TryParse(userIdClaim, out var id))
            {
                loggedUserId = id;
            }

            // Convert JSON request to your existing DTO
            var model = JsonSerializer.Deserialize<MasterUpdateDTO>(
                request.GetRawText(),
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            if (model == null)
            {
                return BadRequest(new
                {
                    message = "Invalid request data."
                });
            }

            if (string.IsNullOrWhiteSpace(model.EntityType))
            {
                return BadRequest(new
                {
                    message = "EntityType is required."
                });
            }

            switch (model.EntityType.Trim().ToLower())
            {
                // =========================================================
                // VAT
                // =========================================================
                case "vat":
                    {
                        var tax = await _context.VAT.FindAsync(model.Id);

                        if (tax == null)
                        {
                            return NotFound(new
                            {
                                message = "Vat not found"
                            });
                        }

                        int activeCount = await _context.VAT
                            .CountAsync(x =>
                                x.Status == "Active" &&
                                x.Id != model.Id);

                        if (model.Status == "Active" && activeCount > 0)
                        {
                            return BadRequest(new
                            {
                                message = "Only one VAT% can be active at a time. Please deactivate the current active VAT% first."
                            });
                        }

                        if (model.Status == "Inactive" && activeCount == 0)
                        {
                            return BadRequest(new
                            {
                                message = "At least one VAT% must remain active. You cannot deactivate the last active VAT%."
                            });
                        }

                        bool taxHasChanges =
                            tax.VATPercentage != model.TaxPercentage ||
                            tax.SatrtDate != model.StartDate ||
                            tax.EndDate != model.EndDate ||
                            tax.Description != model.Description ||
                            tax.Status != model.Status;

                        if (!taxHasChanges)
                        {
                            return BadRequest(new
                            {
                                message = "No changes found."
                            });
                        }

                        tax.VATPercentage = model.TaxPercentage ?? tax.VATPercentage;
                        tax.SatrtDate = model.StartDate ?? tax.SatrtDate;
                        tax.EndDate = model.EndDate ?? tax.EndDate;
                        tax.Description = model.Description;
                        tax.ModifiedDate = DateTime.Now;
                        tax.ModifiedBy = loggedUserId.ToString();
                        tax.Status = model.Status;

                        await _context.SaveChangesAsync();

                        return Ok(new
                        {
                            message = "VAT updated successfully",
                            data = tax
                        });
                    }

                // =========================================================
                // STATUS
                // =========================================================
                case "status":
                    {
                        var status = await _context.Status.FindAsync(model.Id);

                        if (status == null)
                        {
                            return NotFound(new
                            {
                                message = "Status not found"
                            });
                        }

                        status.StatusName = model.StatusName;
                        status.Description = model.Description;
                        status.ModifiedDate = DateTime.Now;
                        status.ModifiedBy = loggedUserId.ToString();

                        await _context.SaveChangesAsync();

                        return Ok(new
                        {
                            message = "Status updated successfully",
                            data = status
                        });
                    }

                // =========================================================
                // CATEGORY
                // =========================================================
                case "category":
                    {
                        var category = await _context.Categories.FindAsync(model.Id);

                        if (category == null)
                        {
                            return NotFound(new
                            {
                                message = "Category not found"
                            });
                        }

                        bool categoryHasChanges =
                            category.Name != model.CategoryName ||
                            category.Status != model.Status;

                        if (!categoryHasChanges)
                        {
                            return BadRequest(new
                            {
                                message = "No changes found."
                            });
                        }

                        category.Name = model.CategoryName;
                        category.Status = model.Status;
                        category.ModifiedDate = DateTime.Now;
                        category.ModifiedBy = loggedUserId.ToString();

                        await _context.SaveChangesAsync();

                        return Ok(new
                        {
                            message = "Category updated successfully",
                            data = category
                        });
                    }

                // =========================================================
                // QUOTATION TYPE
                // =========================================================
                case "quotationtype":
                    {
                        var type = await _context.QuotationType.FindAsync(model.Id);

                        if (type == null)
                        {
                            return NotFound(new
                            {
                                message = "Quotation type not found"
                            });
                        }

                        bool typeHasChanges =
                            type.Name != model.QuotationType ||
                            type.Status != model.Status;

                        if (!typeHasChanges)
                        {
                            return BadRequest(new
                            {
                                message = "No changes found."
                            });
                        }

                        type.Name = model.QuotationType;
                        type.Status = model.Status;
                        type.ModifiedDate = DateTime.Now;
                        type.ModifiedBy = loggedUserId.ToString();

                        await _context.SaveChangesAsync();

                        return Ok(new
                        {
                            message = "Quotation type updated successfully",
                            data = type
                        });
                    }

                // =========================================================
                // METER TYPE
                // =========================================================
                case "metertype":
                    {
                        // IMPORTANT:
                        // MeterType does not exist in MasterUpdateDTO.
                        // Therefore read it directly from the JSON request.

                        string? meterTypeValue = null;

                        if (request.TryGetProperty(
                                "MeterType",
                                out JsonElement meterTypeElement))
                        {
                            meterTypeValue = meterTypeElement.ValueKind ==
                                             JsonValueKind.String
                                ? meterTypeElement.GetString()?.Trim()
                                : null;
                        }

                        if (string.IsNullOrWhiteSpace(meterTypeValue))
                        {
                            return BadRequest(new
                            {
                                message = "Meter Type is required."
                            });
                        }

                        var meterType = await _context.MeterType.FindAsync(model.Id);

                        if (meterType == null)
                        {
                            return NotFound(new
                            {
                                message = "Meter type not found."
                            });
                        }

                        bool meterTypeHasChanges =
                            meterType.Type != meterTypeValue ||
                            meterType.Status != model.Status;

                        if (!meterTypeHasChanges)
                        {
                            return BadRequest(new
                            {
                                message = "No changes found."
                            });
                        }

                        meterType.Type = meterTypeValue;
                        meterType.Status = model.Status;
                        meterType.ModifiedDate = DateTime.Now;
                        meterType.ModifiedBy = loggedUserId.ToString();

                        await _context.SaveChangesAsync();

                        return Ok(new
                        {
                            message = "Meter type updated successfully",
                            data = meterType
                        });
                    }


                // =========================================================
                // HELP
                // =========================================================
                case "help":
                    {
                        if (string.IsNullOrWhiteSpace(model.ScreenName))
                        {
                            return BadRequest(new
                            {
                                message = "Screen Name is required."
                            });
                        }

                        if (string.IsNullOrWhiteSpace(model.Description))
                        {
                            return BadRequest(new
                            {
                                message = "Description is required."
                            });
                        }

                        if (string.IsNullOrWhiteSpace(model.Status))
                        {
                            return BadRequest(new
                            {
                                message = "Status is required."
                            });
                        }

                        var help = await _context.HelpControlTable
                            .FindAsync(model.Id);

                        if (help == null)
                        {
                            return NotFound(new
                            {
                                message = "Help record not found."
                            });
                        }

                        bool helpHasChanges =
                            help.ScreenName != model.ScreenName ||
                            help.Description != model.Description ||
                            help.Status != model.Status;

                        if (!helpHasChanges)
                        {
                            return BadRequest(new
                            {
                                message = "No changes found."
                            });
                        }

                        help.ScreenName = model.ScreenName.Trim();
                        help.Description = model.Description.Trim();
                        help.Status = model.Status.Trim();
                        help.ModifiedDate = DateTime.Now;
                        help.ModifiedBy = loggedUserId.ToString();

                        await _context.SaveChangesAsync();

                        return Ok(new
                        {
                            message = "Help updated successfully",
                            data = help
                        });
                    }
                // =========================================================
                // INVALID ENTITY TYPE
                // =========================================================
                default:
                    {
                        return BadRequest(new
                        {
                            message = "Invalid EntityType. Use: VAT, Status, Category, QuotationType, MeterType,Help"
                        });
                    }
            }
        }


        [HttpGet("getuserslist")]
        public async Task<IActionResult> GetUsersList()
        {

            var userlist = await (
                from u in _context.Users
                join r in _context.Roles on u.RoleId equals r.Id
                join c in _context.Companies on u.CompanyId equals c.Id into compJoin
                from c in compJoin.DefaultIfEmpty()
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
                    u.Status,
                    c.CompanyName,
                    companyId = u.CompanyId,
                    RoleName = r.RoleName
                }).ToListAsync();

            return Ok(new { list = userlist });
        }


        [HttpGet("getcompanieslist")]
        public async Task<IActionResult> GetCompaniesList()
        {

            var entitieslist = await _context.Companies.ToListAsync();

            return Ok(new { list = entitieslist });
        }


        [HttpGet("getRolesList")]
        public async Task<IActionResult> GetRolesList()
        {

            var Roleslist = await _context.Roles.Where(x => x.Status == "Active").ToListAsync();

            return Ok(new { list = Roleslist });
        }

        [HttpGet("getQuotationTypeList")]
        public async Task<IActionResult> getQuotationTypeList()
        {

            var list = await _context.QuotationType.Where(x => x.Status == "Active").ToListAsync();

            return Ok(new { list = list });
        }


    }
}