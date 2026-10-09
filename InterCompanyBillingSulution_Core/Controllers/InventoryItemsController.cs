using InterCompanyBillingSulution.DataAcess.Data;
using InterCompanyBillingSulution.Models;
using InterCompanyBillingSulution.Models.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using OfficeOpenXml;
using OfficeOpenXml.DataValidation;
using System.IO;
using Microsoft.Data.SqlClient;
using System.Data;

namespace InterCompanyBillingSulution_Core.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class InventoryItemsController : ControllerBase
    {
        private readonly InterCompanyBillingDbContext _context;
        private readonly AuditService _auditService;
        private readonly GenerateTemplate _template;

        public InventoryItemsController(InterCompanyBillingDbContext context, AuditService auditService, GenerateTemplate template)
        {
            _context = context;
            _auditService = auditService;
            _template = template;
        }

        [HttpGet("GetTax")]
        public async Task<ActionResult<ApiResponse<IEnumerable<object>>>> GetTax()
        {
            var tax = await _context.VAT.Select(x => x.VATPercentage).FirstOrDefaultAsync();
            return Ok(new { success = true, tax });

        }

        [HttpGet("GetInventoryItems")]

        public async Task<ActionResult<ApiResponse<IEnumerable<object>>>> GetInventoryItems()
        {
            var loggedRole =
                User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;

            bool isSuperAdmin = loggedRole?.Trim().ToLower() == "super admin" || loggedRole?.Trim().ToLower() == "superadmin" || loggedRole?.Trim().ToLower() == "admin";


            var userIdClaim =
                User.Claims.FirstOrDefault(c => c.Type == "userId")?.Value;

            if (!int.TryParse(userIdClaim, out int loggedUserId))
                return Unauthorized("Invalid user");


            // ============================================================
            // BASE QUERY
            // ============================================================

            var itemsQuery =
                from inv in _context.InventoryItem

                join tax in _context.VAT
                    on inv.VAT equals tax.Id.ToString() into taxGroup

                from tx in taxGroup.DefaultIfEmpty()

                join c in _context.Companies
                    on inv.CompanyId equals c.Id.ToString()

                where inv.Status == "Active"

                select new
                {
                    Inventory = inv,
                    Tax = tx,
                    Company = c
                };


            // ============================================================
            // NON-SUPER ADMIN
            // ONLY USER MAPPED COMPANIES
            // ============================================================

            if (!isSuperAdmin)
            {
                var mappedCompanyIds = _context.UserMappedCompanies
                    .Where(x => x.UserId == loggedUserId)
                    .Select(x => x.CompanyId.ToString());

                itemsQuery = itemsQuery
                    .Where(x =>
                        mappedCompanyIds.Contains(x.Inventory.CompanyId));
            }


            // ============================================================
            // GET ITEMS
            // ============================================================

            var items = await itemsQuery
                .Select(x => new
                {
                    x.Inventory.Id,
                    x.Inventory.ItemName,
                    x.Inventory.ItemCode,
                    x.Inventory.Description,
                    x.Inventory.Price,
                    x.Inventory.CompanyId,
                    x.Inventory.Status,

                    TaxRate = x.Tax != null
                        ? x.Tax.VATPercentage
                        : 0,

                    x.Inventory.UnitOfMeasure,
                    x.Inventory.Category,
                    x.Inventory.VatableStatus,
                    x.Company.CompanyName
                })
                .Where(x => x.Status == "Active")
                .ToListAsync();


            return Ok(
                new ApiResponse<IEnumerable<object>>(
                    true,
                    "Items retrieved successfully",
                    items
                )
            );
        }

        [HttpGet("GetTotalInventoryItems")]
        public async Task<IActionResult> GetTotalInventoryItems()
        {
            try
            {
                var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "userId")?.Value;
                var loggedRole = User.Claims
                    .FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;

                var loggedCompanyId = User.Claims
                    .FirstOrDefault(c => c.Type == "companyId")?.Value;

                int loggedUserId = 0;
                if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out var id))
                {
                    loggedUserId = id;
                }


                bool isSuperAdmin = loggedRole == "Super Admin" || loggedRole == "SuperAdmin";

                var items = await _context.InventoryItem
                    .FromSqlInterpolated($@"
                EXEC SP_GetTotalInventoryItems
                    @IsSuperAdmin={isSuperAdmin},
                    @UserId={loggedUserId}")
                    .ToListAsync();

                return Ok(new ApiResponse<IEnumerable<InventoryItem>>(
                    true,
                    "Items retrieved successfully",
                    items));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Success = false,
                    Message = ex.Message,
                    InnerException = ex.InnerException?.Message
                });
            }
        }


        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<InventoryItem>>> GetInventoryItem(int id)
        {
            var item = await _context.InventoryItem.FindAsync(id);
            if (item == null)
                return NotFound(new ApiResponse<InventoryItem>(false, "Item not found"));

            return Ok(new ApiResponse<InventoryItem>(true, "Item retrieved successfully", item));
        }

        //[HttpPost("SaveInventoryItem")]
        //public async Task<ActionResult<ApiResponse<InventoryItem>>> SaveInventoryItem(InventoryItem item)
        //{

        //    var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "userId")?.Value;
        //    var loggedEmail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

        //    int loggedUserId = 0;
        //    if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out var id))
        //    {
        //        loggedUserId = id;
        //    }

        //    var nameExists = _context.InventoryItem.Any(x => x.ItemCode == item.ItemCode);

        //    if (nameExists)
        //    {
        //        //return NotFound(new { message = "Item Name already exists" });
        //        return Ok(new ApiResponse<InventoryItem>(false, "Item Code already exists"));

        //    }
        //    item.CreatedBy = loggedEmail;
        //    item.ModifiedBy = loggedEmail;
        //    item.CreatedDate = DateTime.Now;
        //    item.ModifiedDate = DateTime.Now;
        //    item.IsActive = "Yes";
        //    item.IsDeleted = "No";
        //    item.CompanyId = item.Discount.ToString();
        //    _context.InventoryItem.Add(item);
        //    await _context.SaveChangesAsync();
        //    await _auditService.AddInventoryAudit(item, "Product Created Successfully");

        //    return Ok(new ApiResponse<InventoryItem>(true, "Product Created Successfully", item));
        //}

        //[HttpPost("{id}")]
        //public async Task<ActionResult<ApiResponse<InventoryItem>>> UpdateInventoryItem(int id, InventoryItem item)
        //{
        //    var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "userId")?.Value;
        //    var loggedEmail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;


        //    int loggedUserId = 0;
        //    if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out var Id))
        //    {
        //        loggedUserId = Id;
        //    }

        //    //if (id != item.Id)
        //    //    return BadRequest(new ApiResponse<InventoryItem>(false, "ID mismatch"));

        //    // Get existing record from DB
        //    var existingItem = await _context.InventoryItem.AsNoTracking()
        //            .FirstOrDefaultAsync(x => x.Id == id);

        //    if (existingItem == null)
        //        return NotFound(new ApiResponse<InventoryItem>(false, "Item not found"));

        //    // Compare values — you can ignore fields like ModifiedBy, CreatedBy, CreatedDate, etc.
        //    bool isChanged =
        //        existingItem.ItemCode != item.ItemCode ||
        //        existingItem.ItemName != item.ItemName ||
        //        existingItem.Category != item.Category ||
        //        existingItem.VatableStatus != item.VatableStatus ||
        //        existingItem.Discount != item.Discount ||
        //        existingItem.Description != item.Description ||
        //        existingItem.UnitOfMeasure != item.UnitOfMeasure ||
        //        existingItem.Price != item.Price ||
        //        existingItem.IsActive != item.IsActive ||
        //        existingItem.IsDeleted != item.IsDeleted;


        //    // 🔒 If no changes, just return a message
        //    if (!isChanged)
        //        return Ok(new ApiResponse<InventoryItem>(false, "No Changes Found"));

        //    var nameExists = _context.InventoryItem.Any(x => x.ItemCode == item.ItemCode && x.Id != item.Id);

        //    if (nameExists)
        //    {
        //        return Ok(new ApiResponse<InventoryItem>(false, "Item Code already exists"));

        //    }

        //    // If changed — update and save
        //    item.CompanyId = item.Discount.ToString();
        //    item.ModifiedBy = loggedEmail;

        //    _context.Entry(item).State = EntityState.Modified;

        //    try
        //    {
        //        await _context.SaveChangesAsync();
        //        await _auditService.AddInventoryAudit(item, "Product Updated Successfully");

        //        return Ok(new ApiResponse<InventoryItem>(true, "Product updated successfully", item));
        //    }
        //    catch (DbUpdateConcurrencyException)
        //    {
        //        if (!_context.InventoryItem.Any(e => e.Id == id))
        //            return NotFound(new ApiResponse<InventoryItem>(false, "Product not found"));

        //        throw;
        //    }
        //}

        //[HttpPost("DeleteInventoryItem/{id}")]
        //public async Task<ActionResult<ApiResponse<string>>> DeleteInventoryItem(int id)
        //{
        //    var loggedEmail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

        //    var item = await _context.InventoryItem.FindAsync(id);
        //    if (item == null)
        //        return NotFound(new ApiResponse<string>(false, "Product not found"));
        //    item.IsActive = "No";
        //    item.IsDeleted = "Yes";
        //    item.ModifiedDate = DateTime.Now;
        //    item.ModifiedBy = loggedEmail;
        //    await _context.SaveChangesAsync();
        //    await _auditService.AddInventoryAudit(item, "Product Deleted Successfully");

        //    return Ok(new ApiResponse<string>(true, "Product deleted successfully"));
        //}

        //[HttpPost("SaveInventoryItem")]
        //public async Task<ActionResult<ApiResponse<InventoryItem>>> SaveInventoryItem([FromBody] ProductsDTO item)
        //{
        //    try
        //    {
        //        var loggedEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? "";

        //        if (item == null)
        //        {
        //            return BadRequest(new ApiResponse<InventoryItem>(
        //                false,
        //                "Inventory item data is required."
        //            ));
        //        }

        //        if (!ModelState.IsValid)
        //        {
        //            var errors = ModelState.Values
        //                .SelectMany(v => v.Errors)
        //                .Select(e => e.ErrorMessage);

        //            return BadRequest(new
        //            {
        //                success = false,
        //                message = "Validation failed.",
        //                errors
        //            });
        //        }

        //        await _context.Database.ExecuteSqlRawAsync(
        //            @"EXEC sp_SaveInventoryItem
        //        @ItemCode={0},
        //        @ItemName={1},
        //        @Category={2},
        //        @Description={3},
        //        @UnitOfMeasure={4},
        //        @Price={5},
        //        @CompanyId={6},
        //        @VatableStatus={7},
        //        @CreatedBy={8}",
        //            item.ItemCode,
        //            item.ItemName,
        //            item.Category,
        //            item.Description,
        //            item.UnitOfMeasure,
        //            item.Price,
        //            item.CompanyId,
        //            item.VatableStatus,
        //            loggedEmail
        //        );

        //        var savedItem = await _context.InventoryItem
        //            .OrderByDescending(x => x.Id)
        //            .FirstOrDefaultAsync();

        //        if (savedItem != null)
        //        {
        //            await _auditService.AddInventoryAudit(savedItem, "Product Created Successfully");
        //        }

        //        return Ok(new ApiResponse<InventoryItem>(
        //            true,
        //            "Product Created Successfully",
        //            savedItem
        //        ));
        //    }
        //    catch (SqlException ex)
        //    {
        //        // Log ex

        //        return StatusCode(StatusCodes.Status500InternalServerError,
        //            new ApiResponse<InventoryItem>(
        //                false,
        //                "A database error occurred."
        //            ));
        //    }
        //    catch (DbUpdateException ex)
        //    {
        //        // Log ex

        //        return StatusCode(StatusCodes.Status500InternalServerError,
        //            new ApiResponse<InventoryItem>(
        //                false,
        //                "An error occurred while saving the inventory item."
        //            ));
        //    }
        //    catch (Exception ex)
        //    {
        //        // Log ex

        //        return StatusCode(StatusCodes.Status500InternalServerError,
        //            new ApiResponse<InventoryItem>(
        //                false,
        //                "An unexpected error occurred."
        //            ));
        //    }
        //}
        [HttpPost("SaveInventoryItem")]
        public async Task<ActionResult<ApiResponse<InventoryItem>>> SaveInventoryItem([FromBody] ProductsDTO item)
        {
            try
            {
                var loggedEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? "";

                if (item == null)
                {
                    return BadRequest(new ApiResponse<InventoryItem>(
                        false,
                        "Inventory item data is required."
                    ));
                }

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage);

                    return BadRequest(new
                    {
                        success = false,
                        message = "Validation failed.",
                        errors
                    });
                }

                // Check duplicate Item Code
                bool itemExists = await _context.InventoryItem
                    .AnyAsync(x => x.ItemCode == item.ItemCode && x.Status == "Y");

                if (itemExists)
                {
                    return BadRequest(new ApiResponse<InventoryItem>(
                        false,
                        "Item Code already exists."
                    ));
                }

                await _context.Database.ExecuteSqlRawAsync(
                    @"EXEC sp_SaveInventoryItem
                @ItemCode={0},
                @ItemName={1},
                @Category={2},
                @Description={3},
                @UnitOfMeasure={4},
                @Price={5},
                @CompanyId={6},
                @VatableStatus={7},
                 @MeterTypeId={8},  
                @CreatedBy={9}",
                    item.ItemCode,
                    item.ItemName,
                    item.Category,
                    item.Description,
                    item.UnitOfMeasure,
                    item.Price,
                    item.CompanyId,
                    item.VatableStatus ? "true" : "false",
                    item.MeterTypeId,
                    loggedEmail
                );
                var savedItem = await _context.InventoryItem
                    .OrderByDescending(x => x.Id)
                    .FirstOrDefaultAsync();

                if (savedItem != null)
                {
                    await _auditService.AddInventoryAudit(savedItem, "Product Created Successfully");
                }

                return Ok(new ApiResponse<InventoryItem>(
                    true,
                    "Product Created Successfully",
                    savedItem
                ));
            }
            catch (SqlException ex)
            {
                return BadRequest(new ApiResponse<InventoryItem>(
                    false,
                    ex.Message
                ));
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new ApiResponse<InventoryItem>(
                        false,
                        ex.InnerException?.Message ?? ex.Message
                    ));
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new ApiResponse<InventoryItem>(
                        false,
                        ex.Message
                    ));
            }
        }
        //    [HttpPost("{id}")]
        //    public async Task<ActionResult<ApiResponse<InventoryItem>>> UpdateInventoryItem(int id, ProductsDTO item)
        //    {
        //        var loggedEmail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;


        //        try
        //        {
        //            await _context.Database.ExecuteSqlRawAsync(
        //                @"EXEC SP_UpdateInventoryItem
        //                @Id={0},
        //                @ItemCode={1},
        //                @ItemName={2},
        //                @Category={3},
        //                @Description={4},
        //                @UnitOfMeasure={5},
        //                @Price={6},
        //                @CompanyId={7},
        //                @VatableStatus={8},
        //                @Status={9},
        //                @ModifiedBy={10}",
        //                id,
        //                item.ItemCode,
        //                item.ItemName,
        //                item.Category,
        //                item.Description,
        //                item.UnitOfMeasure,
        //                item.Price,
        //                 item.CompanyId.ToString(),                      // <-- convert to string
        //item.VatableStatus ? "true" : "false",
        //                item.Status,
        //                loggedEmail);

        //            var updatedItem = await _context.InventoryItem
        //                .FirstOrDefaultAsync(x => x.Id == id);

        //            if (updatedItem != null)
        //            {
        //                await _auditService.AddInventoryAudit(updatedItem, "Product Updated Successfully");
        //            }

        //            return Ok(new ApiResponse<InventoryItem>(
        //                true,
        //                "Product Updated Successfully",
        //                updatedItem));
        //        }
        //        catch (Exception ex)
        //        {
        //            return Ok(new ApiResponse<InventoryItem>(false, ex.Message));
        //        }
        //    }

        [HttpPost("{id}")]
        public async Task<ActionResult<ApiResponse<InventoryItem>>> UpdateInventoryItem(int id, ProductsDTO item)
        {
            var loggedEmail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

            if (item == null)
            {
                return BadRequest(new ApiResponse<InventoryItem>(false, "Product data is empty."));
            }

            // Check whether product exists
            var existingItem = await _context.InventoryItem.FirstOrDefaultAsync(x => x.Id == id);

            if (existingItem == null)
            {
                return NotFound(new ApiResponse<InventoryItem>(false, "Product not found."));
            }

            // Normalize vatable status from DTO to the same string form stored in the entity
            //string dtoVatableAsString = item.VatableStatus ? "true" : "false";

            // Check if there are no changes
            bool noChanges =
                existingItem.ItemCode == item.ItemCode &&
                existingItem.ItemName == item.ItemName &&
                existingItem.Category == item.Category &&
                existingItem.Description == item.Description &&
                existingItem.UnitOfMeasure == item.UnitOfMeasure &&
                existingItem.Price == item.Price &&
                 existingItem.CompanyId == item.CompanyId.ToString() && // optional if you want to compare company
                string.Equals(existingItem.VatableStatus, item.VatableStatus.ToString(), StringComparison.OrdinalIgnoreCase) &&
               existingItem.MeterTypeId == item.MeterTypeId?.ToString() &&
                existingItem.Status == item.Status;

            if (noChanges)
            {
                return Ok(new ApiResponse<InventoryItem>(
                    false,
                    "No changes found."));      
            }

            try
            {
                await _context.Database.ExecuteSqlRawAsync(
                    @"EXEC SP_UpdateInventoryItem
            @Id={0},
            @ItemCode={1},
            @ItemName={2},
            @Category={3},
            @Description={4},
            @UnitOfMeasure={5},
            @Price={6},
            @CompanyId={7},
            @VatableStatus={8},
            @MeterTypeId={9},
            @Status={10},
            @ModifiedBy={11}",
                    id,
                    item.ItemCode,
                    item.ItemName,
                    item.Category,
                    item.Description,
                    item.UnitOfMeasure,
                    item.Price,
                    item.CompanyId.ToString(),
                    item.VatableStatus ? "true" : "false",
                    item.MeterTypeId,
                    item.Status,
                    loggedEmail);

                return Ok(new ApiResponse<InventoryItem>(
                    true,
                    "Product Updated Successfully"
                    ));

            }
            catch (Exception ex)
            {
                return Ok(new ApiResponse<InventoryItem>(
                    false,
                    ex.Message));
            }
        }

        [HttpGet("GetTaxes")]

        public async Task<ActionResult> GetTaxes()
        {
            var items = await _context.VAT.Where(s => s.Status == "Active").ToListAsync();
            return Ok(new ApiResponse<IEnumerable<VAT>>(true, "Product retrieved successfully", items));
        }
        [HttpGet("GetMeterTypes")]

        public async Task<ActionResult> GetMeterTypes()
        {
            var meterTypes = await _context.MeterType.Where(s => s.Status == "Active").ToListAsync();
            return Ok(new ApiResponse<IEnumerable<MeterType>>(true, "Product retrieved successfully", meterTypes));
        }
        [HttpGet("GetCategories")]

        public async Task<ActionResult> GetCategories()
        {
            var items = await _context.Categories.Where(s => s.Status == "Active").ToListAsync();
            return Ok(new ApiResponse<IEnumerable<Category>>(true, "Categories retrieved successfully", items));
        }
        [HttpPost("BulkUpload")]
        public async Task<IActionResult> BulkUpload([FromBody] List<InventoryItemDto> items)
        {

            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "userId")?.Value;
            var loggedEmail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

            int loggedUserId = 0;
            if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out var id))
            {
                loggedUserId = id;
            }

            if (items == null || !items.Any())
                return BadRequest("No data received");



            var errors = new List<string>();

            foreach (var item in items)
            {
                if (string.IsNullOrWhiteSpace(item.ItemName))
                {
                    errors.Add($"ItemName missing for ItemCode '{item.ItemCode}'");
                    continue;
                }

                if (item.Price <= 0)
                {
                    errors.Add($"Invalid price for ItemCode '{item.ItemCode}'");
                    continue;
                }

                var exists = _context.InventoryItem
                    .Any(x => x.ItemCode == item.ItemCode);

                if (exists)
                {
                    errors.Add($"Item Code '{item.ItemCode}' already exists");
                    continue;
                }

                var entity = new InventoryItem
                {
                    ItemName = item.ItemName,
                    ItemCode = item.ItemCode,
                    Description = item.Description,
                    Category = item.Category,
                    Price = item.Price,
                    UnitOfMeasure = item.UnitOfMeasure,
                    VatableStatus = item.VatableStatus ? "true" : "false",
                    CreatedDate = DateTime.Now,
                    CreatedBy = loggedEmail,
                    ModifiedBy = loggedEmail,
                    ModifiedDate = DateTime.Now,
                    Status = "Active",
                    CompanyId = item.CompanyId.ToString(),
                };

                _context.InventoryItem.Add(entity);
            }

            // Save once
            await _context.SaveChangesAsync();

            // return errors if any
            if (errors.Any())
            {
                return Ok(new ApiResponse<object>(
                    false,
                    string.Join("\n", errors)
                ));
            }

            return Ok(new ApiResponse<object>(true, "Bulk upload successful"));
        }

        //[HttpGet("download-template")]
        //public  DownloadTemplate()
        //{
        //    var file = _template.GenerateTemplateFile();
        //    return File(file,


        [HttpGet("download-template")]
        public async Task<IActionResult> DownloadTemplate()
        {
            var fileBytes = await _template.GenerateTemplateFile();

            return File(fileBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "ItemTemplate.xlsx");
        }




    }

}