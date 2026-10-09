using InterCompanyBillingSulution.DataAcess.Data;
using InterCompanyBillingSulution.Models;
using InterCompanyBillingSulution.Models.DTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
namespace InterCompanyBillingSulution_Core.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReceivingEntitiesController : ControllerBase
    {
        private readonly InterCompanyBillingDbContext _context;

        public ReceivingEntitiesController(InterCompanyBillingDbContext context)
        {
            _context = context;
        }

        [HttpGet("getEntitiesList")]
        public async Task<IActionResult> GetEntitiesList()
        {

            var entitieslist = await _context.Companies.ToListAsync();

            return Ok(new { list = entitieslist });
        }
        [HttpPost("SaveEntities")]
        public async Task<ActionResult<ApiResponse<Companies>>> SaveRoles(Companies companies)
        {
            // companies.IsActive = "Yes";
            // companies.IsDeleted = "No";
            companies.CreatedBy = "Admin";
            companies.CreatedDate = DateTime.Now;
            companies.ModifiedDate = DateTime.Now;
            companies.ModifiedBy = "Admin";
            _context.Companies.Add(companies);
            await _context.SaveChangesAsync();

            return Ok(new ApiResponse<Companies>(true, "Entities successfully saved", companies));
        }

        [HttpPut("updatecompanies/{id}")]
        public async Task<IActionResult> UpdateCompanies(
    int id,
    [FromForm] CompaniesDTO companiesDto,
    [FromForm(Name = "companyLogo")] IFormFile? companyLogo)
        {
            if (companiesDto == null)
                return BadRequest("Companies data is empty.");

            var existingCompany = await _context.Companies.FindAsync(id);
            if (existingCompany == null)
                return NotFound($"Company with Id {id} not found.");

            // ✅ Handle file upload
            if (companyLogo != null && companyLogo.Length > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "UploadedFiles");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(companyLogo.FileName);
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await companyLogo.CopyToAsync(stream);
                }

                existingCompany.CompanyLogo = fileName;
                existingCompany.FilePath = filePath;
            }

            // ✅ Update text fields
            existingCompany.CompanyName = companiesDto.CompanyName;
            existingCompany.VatNumber = companiesDto.VatNumber;
            existingCompany.CompanyPhoneNumber = companiesDto.CompanyPhoneNumber;
            existingCompany.CompanyAddress = companiesDto.CompanyAddress;
            existingCompany.CompanyEmail = companiesDto.CompanyEmail;
            existingCompany.CompanyWebsite = companiesDto.CompanyWebsite;
            existingCompany.RegistrationNumber = companiesDto.RegistrationNumber;
            existingCompany.BranchCode = companiesDto.BranchCode;
            existingCompany.BrannchAddress = companiesDto.BranchAddress;
            existingCompany.BankAccountNumber = companiesDto.BankAccountNumber;
            existingCompany.IFSCCode = companiesDto.IFSCCode;
            existingCompany.AccountHolderName = companiesDto.AccountHolderName;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Company updated successfully", existingCompany });
        }


        //[HttpDelete("deletecompanies/{id}")]
        //public async Task<IActionResult> DeleteCompanies(int id)
        //{
        //    var company = await _context.Companies.FindAsync(id);
        //    if (company == null)
        //        return NotFound($"Company with Id {id} not found.");

        //    // ✅ If a logo file exists, delete it from server storage
        //    if (!string.IsNullOrEmpty(company.FilePath) && System.IO.File.Exists(company.FilePath))
        //    {
        //        System.IO.File.Delete(company.FilePath);
        //    }

        //    //company.IsDeleted = true;          // ✅ Mark as deleted
        //    company.ModifiedDate = DateTime.UtcNow;
        //    company.ModifiedBy = "Admin";      // replace with logged-in user
        //    _context.Companies.Update(company);

        //    await _context.SaveChangesAsync();

        //    return Ok(new { message = "Company  deleted successfully" });
        //}

        [HttpDelete("deletecompanies/{id}")]
        public async Task<IActionResult> DeleteCompanies(int id)
        {
            var company = await _context.Companies.FindAsync(id);
            if (company == null)
                return NotFound(new { message = $"Company with Id {id} not found." });

            if (!string.IsNullOrEmpty(company.FilePath) && System.IO.File.Exists(company.FilePath))
            {
                System.IO.File.Delete(company.FilePath);
            }

            _context.Companies.Remove(company);   // ✅ Hard delete
            await _context.SaveChangesAsync();

            return Ok(new { message = "Company deleted successfully" });
        }


    }
    }

