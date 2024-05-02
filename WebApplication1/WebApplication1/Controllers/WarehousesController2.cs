using WebApplication1.Models;
using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace WebApplication1.Controllers
{
    [Route("api/warehouses2")]
    [ApiController]
    public class WarehousesController2 : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public WarehousesController2(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpPost]
        public async Task<IActionResult> PostAsync(ElementToPost element)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    await connection.OpenAsync();

                    using (SqlTransaction transaction = connection.BeginTransaction())
                    {
                        if (element.Amount <= 0)
                        {
                            throw new ArgumentException("Amount must be greater than 0.");
                        }

                        SqlCommand command = connection.CreateCommand();
                        command.Transaction = transaction;
                        command.CommandType = CommandType.StoredProcedure;
                        command.CommandText = "AddProductToWarehouse";
                        command.Parameters.AddWithValue("@Amount", element.Amount);
                        command.Parameters.AddWithValue("@IdProduct", element.IdProduct);
                        command.Parameters.AddWithValue("@IdWarehouse", element.IdWarehouse);
                        command.Parameters.AddWithValue("@CreatedAt", element.CreatedAt);

                        await command.ExecuteNonQueryAsync();

                        // Pobranie identyfikatora ostatniego dodanego rekordu
                        command.CommandText = "SELECT IDENT_CURRENT('Product_Warehouse') AS Idx;";
                        object result = await command.ExecuteScalarAsync();
                        var idX = result.ToString();

                        transaction.Commit();
                        return Ok(idX);
                    }
                }
            }
            catch (SqlException sqlError)
            {
                return StatusCode(500, "Database error occurred: " + sqlError.Message);
            }
            catch (ArgumentException argumentError)
            {
                return BadRequest(argumentError.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred: " + ex.Message);
            }
        }
    }
}
