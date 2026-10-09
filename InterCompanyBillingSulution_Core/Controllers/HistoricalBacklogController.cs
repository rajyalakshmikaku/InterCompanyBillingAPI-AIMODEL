using InterCompanyBillingSulution.DataAcess.Data;
using InterCompanyBillingSulution.Models;
using InterCompanyBillingSulution.Models.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Security.Claims;
using System.Text.Json;

namespace InterCompanyBillingSulution_Core.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class HistoricalBacklogController : ControllerBase
    {
        private readonly InterCompanyBillingDbContext _context;

        public HistoricalBacklogController(InterCompanyBillingDbContext context)
        {
            _context = context;
        }

        [HttpPost("HistoricalBacklogBulkUpload")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> HistoricalBacklogBulkUpload([FromForm] HistoricalBacklogUploadRequestDto request)
        {

            try
            {
                var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "userId")?.Value;
                int loggedUserId = 0;
                if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out var id))
                {
                    loggedUserId = id;
                }

                // =========================================================
                // 1. Validate request
                // =========================================================

                if (request == null)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Request is null."
                    });
                }

                if (string.IsNullOrWhiteSpace(request.rows))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Rows data is empty."
                    });
                }


                // =========================================================
                // 2. Deserialize JSON
                // =========================================================

                List<HistoricalBacklog> rows;

                try
                {
                    rows = JsonSerializer.Deserialize<List<HistoricalBacklog>>(
                        request.rows,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                    if (rows == null || rows.Count == 0)
                    {
                        return BadRequest(new
                        {
                            success = false,
                            message = "No records found in JSON."
                        });
                    }
                }
                catch (Exception ex)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "JSON Deserialize Failed.",
                        error = ex.Message
                    });
                }


                // =========================================================
                // 3. Open SQL connection
                // =========================================================

                using SqlConnection con = new SqlConnection(
                    _context.Database.GetDbConnection().ConnectionString
                );

                await con.OpenAsync();


                // =========================================================
                // 4. Clear previous historical backlog
                // =========================================================

                using (SqlCommand truncateCmd =
                       new SqlCommand(
                           "TRUNCATE TABLE dbo.historicalBacklog",
                           con))
                {
                    await truncateCmd.ExecuteNonQueryAsync();
                }


                // =========================================================
                // 5. Insert uploaded rows into historicalBacklog
                // =========================================================

                foreach (var item in rows)
                {
                    string insertSql = @"
                INSERT INTO dbo.historicalBacklog
                (
                    From_Company_Name,
                    From_Address,
                    From_Phone_Number,
                    From_Email,
                    From_VAT_Number,
                    From_Registration_Number,

                    To_Company_Name,
                    To_Address,
                    To_Phone_Number,
                    To_Email,
                    To_VAT_Number,
                    To_Registration_Number,

                    Title,
                    Name,
                    Surname,
                    Email,
                    Role,
                    Gender,
                    Address,
                    Mobile_Number,
                    Username,

                    Item_Name,
                    Description,
                    Currency,
                    Price,
                    Unit_Rate_Per_Item,
                    Quantity,
                    Tax_Percentage,

                    Reference_Number,
                    Quotation_Date,
                    QuotationType,

                    Bank_Account_Number,
                    Bank_Name,
                    Branch_Code,
                    Branch_Address,
                    Bank_Account_Holder_Name,

                    Subtotal,
                    Tax_Amount,
                    Total_Amount,

                    Invoice_Reference_Number,
                    Invoice_Date,

                    Payment_Terms
                )
                VALUES
                (
                    @From_Company_Name,
                    @From_Address,
                    @From_Phone_Number,
                    @From_Email,
                    @From_VAT_Number,
                    @From_Registration_Number,

                    @To_Company_Name,
                    @To_Address,
                    @To_Phone_Number,
                    @To_Email,
                    @To_VAT_Number,
                    @To_Registration_Number,

                    @Title,
                    @Name,
                    @Surname,
                    @Email,
                    @Role,
                    @Gender,
                    @Address,
                    @Mobile_Number,
                    @Username,

                    @Item_Name,
                    @Description,
                    @Currency,
                    @Price,
                    @Unit_Rate_Per_Item,
                    @Quantity,
                    @Tax_Percentage,

                    @Reference_Number,
                    @Quotation_Date,
                    @QuotationType,

                    @Bank_Account_Number,
                    @Bank_Name,
                    @Branch_Code,
                    @Branch_Address,
                    @Bank_Account_Holder_Name,

                    @Subtotal,
                    @Tax_Amount,
                    @Total_Amount,

                    @Invoice_Reference_Number,
                    @Invoice_Date,

                    @Payment_Terms
                )";

                    using SqlCommand cmd = new SqlCommand(insertSql, con);

                    cmd.Parameters.AddWithValue(
                        "@From_Company_Name",
                        item.From_Company_Name ?? "");

                    cmd.Parameters.AddWithValue(
                        "@From_Address",
                        item.From_Address ?? "");

                    cmd.Parameters.AddWithValue(
                        "@From_Phone_Number",
                        item.From_Phone_Number ?? "");

                    cmd.Parameters.AddWithValue(
                        "@From_Email",
                        item.From_Email ?? "");

                    cmd.Parameters.AddWithValue(
                        "@From_VAT_Number",
                        item.From_VAT_Number ?? "");

                    cmd.Parameters.AddWithValue(
                        "@From_Registration_Number",
                        item.From_Registration_Number ?? "");


                    // -----------------------------------------------------
                    // To Company
                    // -----------------------------------------------------

                    cmd.Parameters.AddWithValue(
                        "@To_Company_Name",
                        item.To_Company_Name ?? "");

                    cmd.Parameters.AddWithValue(
                        "@To_Address",
                        item.To_Address ?? "");

                    cmd.Parameters.AddWithValue(
                        "@To_Phone_Number",
                        item.To_Phone_Number ?? "");

                    cmd.Parameters.AddWithValue(
                        "@To_Email",
                        item.To_Email ?? "");

                    cmd.Parameters.AddWithValue(
                        "@To_VAT_Number",
                        item.To_VAT_Number ?? "");

                    cmd.Parameters.AddWithValue(
                        "@To_Registration_Number",
                        item.To_Registration_Number ?? "");


                    // -----------------------------------------------------
                    // Customer
                    // -----------------------------------------------------

                    cmd.Parameters.AddWithValue(
                        "@Title",
                        item.Title ?? "");

                    cmd.Parameters.AddWithValue(
                        "@Name",
                        item.Name ?? "");

                    cmd.Parameters.AddWithValue(
                        "@Surname",
                        item.Surname ?? "");

                    cmd.Parameters.AddWithValue(
                        "@Email",
                        item.Email ?? "");

                    cmd.Parameters.AddWithValue(
                        "@Role",
                        item.Role ?? "");

                    cmd.Parameters.AddWithValue(
                        "@Gender",
                        item.Gender ?? "");

                    cmd.Parameters.AddWithValue(
                        "@Address",
                        item.Address ?? "");

                    cmd.Parameters.AddWithValue(
                        "@Mobile_Number",
                        item.Mobile_Number ?? "");

                    cmd.Parameters.AddWithValue(
                        "@Username",
                        item.Username ?? "");


                    // -----------------------------------------------------
                    // Item
                    // -----------------------------------------------------

                    cmd.Parameters.AddWithValue(
                        "@Item_Name",
                        item.Item_Name ?? "");

                    cmd.Parameters.AddWithValue(
                        "@Description",
                        item.Description ?? "");

                    cmd.Parameters.AddWithValue(
                        "@Currency",
                        item.Currency ?? "");

                    cmd.Parameters.AddWithValue(
                        "@Price",
                        item.Price ?? "");

                    cmd.Parameters.AddWithValue(
                        "@Unit_Rate_Per_Item",
                        item.Unit_Rate_Per_Item ?? "");

                    cmd.Parameters.AddWithValue(
                        "@Quantity",
                        item.Quantity ?? "");

                    cmd.Parameters.AddWithValue(
                        "@Tax_Percentage",
                        item.Tax_Percentage ?? "");


                    // -----------------------------------------------------
                    // Quotation
                    // -----------------------------------------------------

                    cmd.Parameters.AddWithValue(
                        "@Reference_Number",
                        item.Reference_Number ?? "");

                    cmd.Parameters.AddWithValue(
                        "@Quotation_Date",
                        item.Quotation_Date ?? "");

                    cmd.Parameters.AddWithValue(
                        "@QuotationType",
                        item.QuotationType ?? "");


                    // -----------------------------------------------------
                    // Bank
                    // -----------------------------------------------------

                    cmd.Parameters.AddWithValue(
                        "@Bank_Account_Number",
                        item.Bank_Account_Number ?? "");

                    cmd.Parameters.AddWithValue(
                        "@Bank_Name",
                        item.Bank_Name ?? "");

                    cmd.Parameters.AddWithValue(
                        "@Branch_Code",
                        item.Branch_Code ?? "");

                    cmd.Parameters.AddWithValue(
                        "@Branch_Address",
                        item.Branch_Address ?? "");

                    cmd.Parameters.AddWithValue(
                        "@Bank_Account_Holder_Name",
                        item.Bank_Account_Holder_Name ?? "");


                    // -----------------------------------------------------
                    // Amounts
                    // -----------------------------------------------------

                    cmd.Parameters.AddWithValue(
                        "@Subtotal",
                        item.Subtotal ?? "");

                    cmd.Parameters.AddWithValue(
                        "@Tax_Amount",
                        item.Tax_Amount ?? "");

                    cmd.Parameters.AddWithValue(
                        "@Total_Amount",
                        item.Total_Amount ?? "");


                    // -----------------------------------------------------
                    // Invoice
                    // -----------------------------------------------------

                    cmd.Parameters.AddWithValue(
                        "@Invoice_Reference_Number",
                        item.Invoice_Reference_Number ?? "");

                    cmd.Parameters.AddWithValue(
                        "@Invoice_Date",
                        item.Invoice_Date ?? "");


                    // -----------------------------------------------------
                    // Payment
                    // -----------------------------------------------------

                    cmd.Parameters.AddWithValue(
                        "@Payment_Terms",
                        item.Payment_Terms ?? "");


                    // Execute INSERT
                    await cmd.ExecuteNonQueryAsync();
                }


                // =========================================================
                // 6. Migrate historicalBacklog into individual tables
                // =========================================================

                // =========================================================
                // 6. Migrate historicalBacklog into individual tables
                // =========================================================

                using (SqlCommand migrateCmd =
                       new SqlCommand("dbo.usp_MigrateHistoricalUploads", con))
                {
                    migrateCmd.CommandType = CommandType.StoredProcedure;

                    // Input parameters
                    migrateCmd.Parameters.Add(
                        "@DryRun",
                        SqlDbType.Bit
                    ).Value = false;

                    migrateCmd.Parameters.Add(
                        "@DefaultUserId",
                        SqlDbType.Int
                    ).Value = 1;

                    migrateCmd.Parameters.Add(
                        "@DefaultUserNm",
                        SqlDbType.NVarChar,
                        255
                    ).Value = loggedUserId;

                    migrateCmd.Parameters.Add(
                        "@ClearStagingAfterImport",
                        SqlDbType.Bit
                    ).Value = true;

                    migrateCmd.Parameters.Add(
                        "@HeaderRowOffset",
                        SqlDbType.Int
                    ).Value = 2;


                    // =====================================================
                    // Output parameters
                    // =====================================================

                    SqlParameter successParam = migrateCmd.Parameters.Add(
                        "@Success",
                        SqlDbType.Bit
                    );
                    successParam.Direction = ParameterDirection.Output;


                    SqlParameter errorCountParam = migrateCmd.Parameters.Add(
                        "@ErrorCount",
                        SqlDbType.Int
                    );
                    errorCountParam.Direction = ParameterDirection.Output;


                    SqlParameter warningCountParam = migrateCmd.Parameters.Add(
                        "@WarningCount",
                        SqlDbType.Int
                    );
                    warningCountParam.Direction = ParameterDirection.Output;


                    SqlParameter documentsImportedParam = migrateCmd.Parameters.Add(
                        "@DocumentsImported",
                        SqlDbType.Int
                    );
                    documentsImportedParam.Direction = ParameterDirection.Output;


                    SqlParameter rowsImportedParam = migrateCmd.Parameters.Add(
                        "@RowsImported",
                        SqlDbType.Int
                    );
                    rowsImportedParam.Direction = ParameterDirection.Output;


                    SqlParameter summaryParam = migrateCmd.Parameters.Add(
                        "@Summary",
                        SqlDbType.NVarChar,
                        1000
                    );
                    summaryParam.Direction = ParameterDirection.Output;


                    // =====================================================
                    // Execute Stored Procedure
                    // =====================================================

                    await migrateCmd.ExecuteNonQueryAsync();


                    // =====================================================
                    // Read output values
                    // =====================================================

                    bool success =
                        successParam.Value != DBNull.Value &&
                        Convert.ToBoolean(successParam.Value);

                    int errorCount =
                        errorCountParam.Value != DBNull.Value
                            ? Convert.ToInt32(errorCountParam.Value)
                            : 0;

                    int warningCount =
                        warningCountParam.Value != DBNull.Value
                            ? Convert.ToInt32(warningCountParam.Value)
                            : 0;

                    int documentsImported =
                        documentsImportedParam.Value != DBNull.Value
                            ? Convert.ToInt32(documentsImportedParam.Value)
                            : 0;

                    int rowsImported =
                        rowsImportedParam.Value != DBNull.Value
                            ? Convert.ToInt32(rowsImportedParam.Value)
                            : 0;

                    string summary =
                        summaryParam.Value != DBNull.Value
                            ? summaryParam.Value.ToString()
                            : "Migration completed.";


                    // =====================================================
                    // Migration failed / validation errors
                    // =====================================================

                    if (!success || errorCount > 0)
                    {
                        return BadRequest(new
                        {
                            success = false,
                            message = summary,
                            errorCount = errorCount,
                            warningCount = warningCount,
                            documentsImported = documentsImported,
                            rowsImported = rowsImported
                        });
                    }


                    // =====================================================
                    // Migration successful
                    // =====================================================

                    return Ok(new
                    {
                        success = true,
                        message = summary,
                        warningCount = warningCount,
                        documentsImported = documentsImported,
                        rowsImported = rowsImported
                    });
                }


                // =========================================================
                // 7. Close connection
                // =========================================================

                await con.CloseAsync();


                // =========================================================
                // 8. Success
                // =========================================================

                return Ok(new
                {
                    success = true,
                    message =
                        $"{rows.Count} record(s) uploaded and migrated successfully."
                });
            }
            catch (SqlException ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Database error occurred.",
                    error = ex.Message,
                    inner = ex.InnerException?.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Historical backlog migration failed.",
                    error = ex.Message,
                    inner = ex.InnerException?.Message
                });
            }
        }
    }
}