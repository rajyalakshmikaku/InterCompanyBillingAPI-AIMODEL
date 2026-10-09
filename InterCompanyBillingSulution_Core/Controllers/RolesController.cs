using InterCompanyBillingSulution.DataAcess.Data;
using InterCompanyBillingSulution.Models;
using InterCompanyBillingSulution.Models.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
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
    public class RolesController : ControllerBase
    {
        private readonly InterCompanyBillingDbContext _context;
        private readonly AuditService _auditService;

        public RolesController(InterCompanyBillingDbContext context, AuditService auditService)
        {
            _context = context;
            _auditService = auditService;

        }


        //[HttpGet("getRolesList")]
        //public async Task<IActionResult> GetRolesList()
        //{
        //    List<Roles> list = new();

        //    using (SqlConnection con = new SqlConnection(_context.Database.GetDbConnection().ConnectionString))
        //    {
        //        using (SqlCommand cmd = new SqlCommand("sp_GetRolesList", con))
        //        {
        //            cmd.CommandType = CommandType.StoredProcedure;

        //            await con.OpenAsync();

        //            SqlDataReader dr = await cmd.ExecuteReaderAsync();

        //            while (await dr.ReadAsync())
        //            {
        //                list.Add(new Roles
        //                {
        //                    Id = Convert.ToInt32(dr["Id"]),
        //                    RoleName = dr["RoleName"].ToString(),
        //                    RoleDescription = dr["RoleDescription"].ToString(),
        //                    Status = dr["Status"].ToString(),
        //                    //IsActive = dr["IsActive"].ToString(),
        //                    //IsDeleted = dr["IsDeleted"].ToString(),
        //                    CreatedBy = dr["CreatedBy"].ToString(),
        //                    CreatedDate = Convert.ToDateTime(dr["CreatedDate"]),
        //                    ModifiedBy = dr["ModifiedBy"].ToString(),
        //                    ModifiedDate = Convert.ToDateTime(dr["ModifiedDate"])

        //                });
        //            }
        //        }
        //    }

        //    return Ok(new { list });
        //}
        [HttpGet("getRolesList")]
        public async Task<IActionResult> GetRolesList()
        {
            var role = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;

            var companyId = User.Claims.FirstOrDefault(c => c.Type == "companyId")?.Value;

            bool isSuperAdmin = role == "Super Admin" || role == "SuperAdmin";

            int? companyIdValue = null;

            if (int.TryParse(companyId, out int parsedCompanyId))
            {
                companyIdValue = parsedCompanyId;
            }

            List<Roles> list = new();

            using (SqlConnection con = new SqlConnection(
                _context.Database.GetDbConnection().ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand("sp_GetRolesList", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.Add("@IsSuperAdmin", SqlDbType.Bit).Value = isSuperAdmin;

                    cmd.Parameters.Add("@CompanyId", SqlDbType.Int).Value = companyIdValue.HasValue? companyIdValue.Value: DBNull.Value;

                    await con.OpenAsync();

                    using (SqlDataReader dr = await cmd.ExecuteReaderAsync())
                    {
                        while (await dr.ReadAsync())
                        {
                            list.Add(new Roles
                            {
                                Id = Convert.ToInt32(dr["Id"]),
                                RoleName = dr["RoleName"]?.ToString(),
                                RoleDescription = dr["RoleDescription"]?.ToString(),
                                Status = dr["Status"]?.ToString(),
                                CreatedBy = dr["CreatedBy"]?.ToString(),
                                CreatedDate = Convert.ToDateTime(dr["CreatedDate"]),
                                ModifiedBy = dr["ModifiedBy"]?.ToString(),
                                ModifiedDate = Convert.ToDateTime(dr["ModifiedDate"])
                            });
                        }
                    }
                }
            }

            return Ok(new { list });
        }
        [HttpPost("SaveRoles")]
        public async Task<IActionResult> SaveRoles(RolesDTO role)
        {
            try
            {
                var email = User.FindFirst(ClaimTypes.Email)?.Value;
                var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

                using SqlConnection con = new(_context.Database.GetDbConnection().ConnectionString);
                using SqlCommand cmd = new("SP_SaveRoles", con);

                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@RoleName", role.RoleName);
                cmd.Parameters.AddWithValue("@RoleDescription", role.RoleDescription);
                cmd.Parameters.AddWithValue("@CreatedBy", email ?? "");
                cmd.Parameters.AddWithValue("@IPAddress", ip ?? "");

                await con.OpenAsync();

                using SqlDataReader reader = await cmd.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    bool success = Convert.ToBoolean(reader["Success"]);
                    string message = reader["Message"]?.ToString();
                    int? roleId = reader["RoleId"] == DBNull.Value
                        ? null
                        : Convert.ToInt32(reader["RoleId"]);

                    if (success)
                    {
                        role.Id = roleId ?? 0;

                        return Ok(new ApiResponse<RolesDTO>(
                            true,
                            message,
                            role
                        ));
                    }

                    return Ok(new ApiResponse<Roles>(
                        false,
                        message
                    ));
                }

                return Ok(new ApiResponse<Roles>(
                    false,
                    "No response received from stored procedure."
                ));
            }
            catch (Exception ex)
            {
                return StatusCode(500,
                    new ApiResponse<Roles>(
                        false,
                        ex.Message
                    ));
            }
        }
        [HttpPost("UpdateRoles/{id}")]
        public async Task<ActionResult<ApiResponse<string>>> UpdateRoles(int id, [FromBody] RolesDTO role)
        {
            if (id != role.Id)
                return BadRequest(new ApiResponse<string>(false, "ID mismatch"));

            try
            {
                var loggedEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? "";
                var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";

                using SqlConnection con = new(_context.Database.GetDbConnection().ConnectionString);
                using SqlCommand cmd = new("SP_UpdateRoles", con);

                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@Id", role.Id);
                cmd.Parameters.AddWithValue("@RoleName", role.RoleName);
                cmd.Parameters.AddWithValue("@RoleDescription", role.RoleDescription);
                cmd.Parameters.AddWithValue("@Status", role.Status);
                cmd.Parameters.AddWithValue("@ModifiedBy", loggedEmail);
                cmd.Parameters.AddWithValue("@IPAddress", ip);

                await con.OpenAsync();

                using SqlDataReader reader = await cmd.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    bool success = Convert.ToBoolean(reader["Success"]);
                    string message = reader["Message"]?.ToString() ?? "";

                    return Ok(new ApiResponse<string>(
                        success,
                        message
                    ));
                }

                return Ok(new ApiResponse<string>(
                    false,
                    "No response received from stored procedure."
                ));
            }
            catch (Exception ex)
            {
                return StatusCode(500,
                    new ApiResponse<string>(
                        false,
                        ex.Message
                    ));
            }
        }
        [HttpPost("DeleteRoles/{id}")]
        public async Task<IActionResult> DeleteRoles(int id)
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

            using SqlConnection con = new(_context.Database.GetDbConnection().ConnectionString);
            using SqlCommand cmd = new("SP_DeleteRoles", con);

            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@Id", id);
            cmd.Parameters.AddWithValue("@ModifiedBy", email ?? "");
            cmd.Parameters.AddWithValue("@IPAddress", ip ?? "");

            await con.OpenAsync();
            await cmd.ExecuteNonQueryAsync();

            return Ok(new ApiResponse<string>(true, "Role deleted successfully."));
        }



    }

}