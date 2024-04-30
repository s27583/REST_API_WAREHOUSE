using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Data;
using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using REST_API_WAREHOUSE.DTOs;


namespace REST_API_WAREHOUSE.Controllers;

[ApiController]
[Route("[controller]")]
public class WarehouseController : ControllerBase
{
    private readonly SqlConnection _connection;

    public WarehouseController(SqlConnection connection)
    {
        _connection = connection;
    }

    [HttpPost]
    public async Task<IActionResult> AddProductToWarehouse([FromBody] ProductWarehouseDto dto)
    {
        try
        {

            if (dto.Amount <= 0)
            {
                return BadRequest("Amount must be greater than 0");
            }

            await _connection.OpenAsync();

            var productCommand = new SqlCommand("SELECT * FROM Product WHERE Id = @Id", _connection);
            productCommand.Parameters.AddWithValue("@Id", dto.ProductId);
            var productExists = await productCommand.ExecuteReaderAsync();
            if (!productExists.HasRows)
            {
                return NotFound("Product not found");
            }
            await productExists.CloseAsync();

            var warehouseCommand = new SqlCommand("SELECT * FROM Warehouse WHERE Id = @Id", _connection);
            warehouseCommand.Parameters.AddWithValue("@Id", dto.WarehouseId);
            var warehouseExists = await warehouseCommand.ExecuteReaderAsync();
            if (!warehouseExists.HasRows)
            {
                return NotFound("Warehouse not found");
            }
            await warehouseExists.CloseAsync();

            var orderCommand = new SqlCommand("SELECT * FROM PurchaseOrder WHERE ProductId = @ProductId AND Amount = @Amount AND CreatedAt < @CreatedAt AND FulfilledAt IS NULL", _connection);
            orderCommand.Parameters.AddWithValue("@ProductId", dto.ProductId);
            orderCommand.Parameters.AddWithValue("@Amount", dto.Amount);
            orderCommand.Parameters.AddWithValue("@CreatedAt", dto.CreatedAt);
            var orderExists = await orderCommand.ExecuteReaderAsync();
            if (!orderExists.HasRows)
            {
                return BadRequest("No matching unfulfilled order found");
            }
            await orderExists.CloseAsync();

            var updateOrderCommand = new SqlCommand("UPDATE PurchaseOrder SET FulfilledAt = @FulfilledAt WHERE ProductId = @ProductId AND Amount = @Amount AND CreatedAt < @CreatedAt AND FulfilledAt IS NULL", _connection);
            updateOrderCommand.Parameters.AddWithValue("@FulfilledAt", DateTime.UtcNow);
            updateOrderCommand.Parameters.AddWithValue("@ProductId", dto.ProductId);
            updateOrderCommand.Parameters.AddWithValue("@Amount", dto.Amount);
            updateOrderCommand.Parameters.AddWithValue("@CreatedAt", dto.CreatedAt);
            await updateOrderCommand.ExecuteNonQueryAsync();

            var insertCommand = new SqlCommand("INSERT INTO Product_Warehouse (ProductId, WarehouseId, Price, CreatedAt) VALUES (@ProductId, @WarehouseId, @Price, @CreatedAt); SELECT SCOPE_IDENTITY()", _connection);
            insertCommand.Parameters.AddWithValue("@ProductId", dto.ProductId);
            insertCommand.Parameters.AddWithValue("@WarehouseId", dto.WarehouseId);
            insertCommand.Parameters.AddWithValue("@Price", (decimal)dto.Price * (decimal)dto.Amount);
            insertCommand.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow);
            var insertedId = await insertCommand.ExecuteScalarAsync();

            return Ok($"Product added to warehouse successfully with ID: {insertedId}");
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
        finally
        {
            _connection.Close();
        }
    }
}
