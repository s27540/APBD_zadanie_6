using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    [Route("api/warehouses")]
    [ApiController]
    public class WarehousesController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        
        public WarehousesController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpPost]
        public async Task<IActionResult> Post(ElementToPost element)
        {
            if (element.Amount <= 0)
            {
                return BadRequest("Invalid parameter: Amount should be greater than 0.");
            }

            using (SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                await connection.OpenAsync();

                SqlTransaction transaction = connection.BeginTransaction();

                try
                {
                    var orders = new List<Order>();
                    var productsFromWarehouse = new List<ProductWarehouse>();
                    var products = new List<Product>();

                    using (var com = new SqlCommand("SELECT * FROM [Order] WHERE IdProduct = @IdProduct AND Amount = @Amount AND CreatedAt < @CreatedAt;", connection, transaction))
                    {
                        com.Parameters.AddWithValue("@IdProduct", element.IdProduct);
                        com.Parameters.AddWithValue("@Amount", element.Amount);
                        com.Parameters.AddWithValue("@CreatedAt", element.CreatedAt);

                        var dr = await com.ExecuteReaderAsync();
                        while (await dr.ReadAsync())
                        {
                            orders.Add(new Order
                            {
                                IdOrder = (int)dr["IdOrder"],
                                IdProduct = (int)dr["IdProduct"],
                                Amount = (int)dr["Amount"],
                                CreatedAt = (DateTime)dr["CreatedAt"]
                            });
                        }
                        await dr.CloseAsync();
                    }

                    if (orders.Count == 0)
                    {
                        return NotFound("Invalid parameter: There is no matching order.");
                    }

                    using (var com2 = new SqlCommand("SELECT * FROM Product_Warehouse WHERE IdOrder = @IdOrder;", connection, transaction))
                    {
                        com2.Parameters.AddWithValue("@IdOrder", orders.First().IdOrder);

                        SqlDataReader dr2 = await com2.ExecuteReaderAsync();
                        while (await dr2.ReadAsync())
                        {
                            productsFromWarehouse.Add(new ProductWarehouse
                            {
                                IdOrder = (int)dr2["IdOrder"]
                            });
                        }
                        await dr2.CloseAsync();
                    }

                    if (productsFromWarehouse.Count > 0)
                    {
                        return BadRequest("Invalid parameter: The order has already been fulfilled.");
                    }

                    using (var com3 = new SqlCommand("SELECT * FROM Product WHERE IdProduct = @IdProduct;", connection, transaction))
                    {
                        com3.Parameters.AddWithValue("@IdProduct", element.IdProduct);

                        SqlDataReader dr3 = await com3.ExecuteReaderAsync();
                        while (await dr3.ReadAsync())
                        {
                            products.Add(new Product
                            {
                                IdProduct = (int)dr3["IdProduct"]
                            });
                        }
                        await dr3.CloseAsync();
                    }

                    if (products.Count == 0)
                    {
                        return NotFound("Invalid parameter: Product with provided IdProduct does not exist.");
                    }

                    var updateCommand = new SqlCommand("UPDATE [Order] SET FulfilledAt = @CurrentDate WHERE IdOrder = @IdOrder;", connection, transaction);
                    updateCommand.Parameters.AddWithValue("@CurrentDate", DateTime.Now);
                    updateCommand.Parameters.AddWithValue("@IdOrder", orders.First().IdOrder);
                    await updateCommand.ExecuteNonQueryAsync();

                    var insertCommand = new SqlCommand("INSERT INTO Product_Warehouse(IdWarehouse, IdProduct, IdOrder, Amount, Price, CreatedAt) VALUES(@IdWarehouse, @IdProduct, @IdOrder, @Amount, @Price, @CreatedAt);", connection, transaction);
                    insertCommand.Parameters.AddWithValue("@IdWarehouse", element.IdWarehouse);
                    insertCommand.Parameters.AddWithValue("@IdProduct", element.IdProduct);
                    insertCommand.Parameters.AddWithValue("@IdOrder", orders.First().IdOrder);
                    insertCommand.Parameters.AddWithValue("@Amount", element.Amount);
                    insertCommand.Parameters.AddWithValue("@Price", products.First().Price); // Assuming product price is fetched from database
                    insertCommand.Parameters.AddWithValue("@CreatedAt", DateTime.Now);
                    await insertCommand.ExecuteNonQueryAsync();

                    transaction.Commit();
                    return Ok("Order fulfilled successfully.");
                }
                catch (SqlException sqlError)
                {
                    await transaction.RollbackAsync();
                    Console.WriteLine(sqlError.Message + " " + sqlError.LineNumber);
                    return StatusCode(500, "Database error occurred.");
                }
            }
        }
    }
}
