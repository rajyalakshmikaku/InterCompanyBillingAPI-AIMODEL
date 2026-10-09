using InterCompanyBillingSulution.DataAcess.Data;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using OfficeOpenXml.DataValidation;
using OfficeOpenXml.Style;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace InterCompanyBillingSulution.Models.DTO
{
    public class GenerateTemplate
    {

        private readonly InterCompanyBillingDbContext _context;

        public GenerateTemplate(InterCompanyBillingDbContext context)
        {
            _context = context; // ✅ FIX
        }


        public async Task<byte[]> GenerateTemplateFile()
        {
            using (var package = new ExcelPackage())
            {
                var sheet = package.Workbook.Worksheets.Add("Template");

                // ================= HEADERS =================
                string[] headers = {"Item Code", "Item Name", "Item Description", "Category", "Price Excl VAT", "Unit of Measure", "Company", "Is Vatable"};

                for (int i = 0; i < headers.Length; i++)
                {
                    sheet.Cells[1, i + 1].Value = headers[i];
                }

                using (var range = sheet.Cells["A1:H1"])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                }

                sheet.View.FreezePanes(2, 1);
                sheet.Cells["A1:H1"].AutoFilter = true;

                // ================= FETCH DATA =================

                var categories = await _context.Categories
                    .Where(x => x.Status == "Y")
                    .Select(x => x.Name) // make sure this column exists
                    .ToListAsync();

                var companies = await _context.Companies
                    .Where(x => x.Status == "Y")
                    .Select(x => new { x.Id, x.CompanyName })
                    .ToListAsync();

                // ================= SAFE FALLBACK =================

                if (!categories.Any())
                {
                    categories.Add("No Data");
                }

                if (!companies.Any())
                {
                    companies.Add(new { Id = 0, CompanyName = "No Data" });
                }

                // ================= LOOKUP SHEET =================

                var lookup = package.Workbook.Worksheets.Add("Lookup");

                // Load data efficiently
                lookup.Cells["A1"].LoadFromCollection(categories);
                lookup.Cells["B1"].LoadFromCollection(companies.Select(x => x.CompanyName));
                lookup.Cells["C1"].LoadFromCollection(companies.Select(x => x.Id));

                lookup.Hidden = eWorkSheetHidden.VeryHidden;

                // ================= NAMED RANGES =================

                var categoryRange = lookup.Cells[1, 1, categories.Count, 1];
                var companyRange = lookup.Cells[1, 2, companies.Count, 2];

                // Remove existing (avoid duplicate error)
                if (package.Workbook.Names.Any(n => n.Name == "CategoryList"))
                {
                    package.Workbook.Names.Remove("CategoryList");
                }
                if (package.Workbook.Names.Any(n => n.Name == "CompanyList"))
                {
                    package.Workbook.Names.Remove("CompanyList");
                }
                // Add new named ranges
                package.Workbook.Names.Add("CategoryList", categoryRange);
                package.Workbook.Names.Add("CompanyList", companyRange);

                // ================= DROPDOWNS =================

                //var catValidation = sheet.DataValidations.AddListValidation("D2:D100");
                //catValidation.Formula.ExcelFormula = "CategoryList";
                //catValidation.ShowErrorMessage = true;
                //catValidation.Error = "Select valid category";

                //var compValidation = sheet.DataValidations.AddListValidation("G2:G100");
                //compValidation.Formula.ExcelFormula = "CompanyList";
                //compValidation.ShowErrorMessage = true;
                //compValidation.Error = "Select valid company";
                // ================= CATEGORY =================
                var catValidation = sheet.DataValidations.AddListValidation("D2:D100");
                catValidation.Formula.ExcelFormula = "CategoryList";

                // ❌ Error message (when wrong input)
                catValidation.ShowErrorMessage = true;
                catValidation.ErrorTitle = "Invalid Category";
                catValidation.Error = "Please select a valid Category from dropdown only";

                // ✅ Prompt (when user clicks cell)
                catValidation.ShowInputMessage = true;
                catValidation.PromptTitle = "Category";
                catValidation.Prompt = "Select Category from dropdown";

                // ✅ Make mandatory
                catValidation.AllowBlank = false;


                // ================= COMPANY =================
                var compValidation = sheet.DataValidations.AddListValidation("G2:G100");
                compValidation.Formula.ExcelFormula = "CompanyList";

                // ❌ Error message
                compValidation.ShowErrorMessage = true;
                compValidation.ErrorTitle = "Invalid Company";
                compValidation.Error = "Please select a valid Company from dropdown only";

                // ✅ Prompt
                compValidation.ShowInputMessage = true;
                compValidation.PromptTitle = "Company";
                compValidation.Prompt = "Select Company from dropdown";

                // ✅ Mandatory
                compValidation.AllowBlank = false;
                // Static dropdowns
                var unitValidation = sheet.DataValidations.AddListValidation("F2:F100");
                unitValidation.Formula.Values.Add("Hour");
                unitValidation.Formula.Values.Add("Unit");

                var vatValidation = sheet.DataValidations.AddListValidation("H2:H100");
                vatValidation.Formula.Values.Add("TRUE");
                vatValidation.Formula.Values.Add("FALSE");
                var priceValidation = sheet.DataValidations.AddDecimalValidation("E2:E100");

                //Only allow numbers >= 0
                priceValidation.Operator = ExcelDataValidationOperator.greaterThanOrEqual;
                priceValidation.Formula.Value = 0;

                //Error message
                priceValidation.ShowErrorMessage = true;
                priceValidation.ErrorTitle = "Invalid Price";
                priceValidation.Error = "Only numeric values are allowed for Price Excl VAT";

                //Prompt message
                priceValidation.ShowInputMessage = true;
                priceValidation.PromptTitle = "Price Excl VAT";
                priceValidation.Prompt = "Enter Price Excl VAT numeric value only";

                priceValidation.AllowBlank = false;
                // ================= SAMPLE ROW =================

                sheet.Cells[2, 6].Value = "Hour";
                sheet.Cells[2, 8].Value = "TRUE";

                // ================= FORMATTING =================

                sheet.Cells["A1:H1"].AutoFitColumns();
                for (int i = 1; i <= 8; i++)
                {
                    sheet.Column(i).Width += 2;
                }

                // ================= PROTECTION =================

                sheet.Cells.Style.Locked = true;
                sheet.Cells["A2:H100"].Style.Locked = false;

                sheet.Protection.IsProtected = true;
                sheet.Protection.AllowSelectUnlockedCells = true;
                sheet.Protection.AllowFormatColumns = true;

                return package.GetAsByteArray();
            }
        }

    }
}
