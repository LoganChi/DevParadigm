using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DevParadigm.Basement.Handler;
using DevParadigm.Basement.Units;
using DevParadigm.Example.DTOs.Input;
using DevParadigm.Example.DTOs.Output;
using DevParadigm.Example.Entities;
using DevParadigm.Example.Extensions;
using DevParadigm.Example.DbContexts;
using DevParadigm.Interface;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Data.Sqlite;

namespace DevParadigm.Test;

public class OrderIntegrationTests : IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly SqliteConnection _connection;

    public OrderIntegrationTests()
    {
        // Force load DevParadigm assembly for service scanning
        var _ = typeof(DevParadigm.Infrastructure.UnifiedGradedValidator);

        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        // Setup SQLite In-Memory
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        // Register services with SQLite
        services.AddExampleLayer(configuration, (options, config) =>
        {
            options.UseSqlite(_connection); // Use same DB for Read and Write contexts
        });

        _serviceProvider = services.BuildServiceProvider();
        
        // Ensure Database Created
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DevParadigm.Example.DbContexts.WriteDbContext>();
        dbContext.Database.EnsureDeleted();
        dbContext.Database.EnsureCreated();
        
        // Seed initial data
        SeedData();
    }
    
    public void Dispose()
    {
        _connection.Dispose();
    }

    private void SeedData()
    {
        using var scope = _serviceProvider.CreateScope();
        // Actually AddExampleLayer registers WriteDbContext and ReadDbContext separately.
        // Let's get WriteDbContext specifically.
        var dbContext = scope.ServiceProvider.GetRequiredService<DevParadigm.Example.DbContexts.WriteDbContext>();

        // Seed Merchant
        var merchantId = DevParadigm.Example.Utils.EntityBuilders.OrderEntityBuilder.DefaultMerchantId;
        if (dbContext.Set<Merchant>().Find(merchantId) == null)
        {
            dbContext.Set<Merchant>().Add(new Merchant
            {
                Id = merchantId,
                Name = "Default Merchant",
                Status = 0
            });
        }

        // Seed Product/Stock
        var productId = Guid.Parse("12345678-1234-1234-1234-1234567890AB");
        if (dbContext.Set<Stock>().Find(productId) == null)
        {
            dbContext.Set<Stock>().Add(new Stock
            {
                ProductId = productId,
                RemainQuantity = 100,
                UpdateTime = DateTime.UtcNow
            });
        }

        dbContext.SaveChanges();
    }

    [Fact]
    public async Task CreateOrder_Success_ShouldReturnOrderId()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<IBusinessHandler<CreateOrderInput, CreateOrderOutput>>();
        var input = new CreateOrderInput
        {
            UserId = "U123456",
            ProductId = Guid.Parse("12345678-1234-1234-1234-1234567890AB"),
            Quantity = 2,
            Remark = "Valid order"
        };
        var userInfo = new UserInfo("U123456", "TestUser", 1);

        // Act
        var result = await handler.ExecuteAsync(input, userInfo, CancellationToken.None);

        // Assert
        Assert.True(result.Success, $"Handler failed with: {string.Join(", ", result.Errors ?? new List<string>())}");
        Assert.NotNull(result.Data);
        Assert.NotEqual(Guid.Empty, result.Data.OrderId);
        Assert.StartsWith("ORD", result.Data.OrderNo);
    }

    [Fact]
    public async Task CreateOrder_InsufficientStock_ShouldFail()
    {
        // Arrange
        // Update stock to be low for this test
        using (var scope = _serviceProvider.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<DevParadigm.Example.DbContexts.WriteDbContext>();
            var stock = dbContext.Set<Stock>().Find(Guid.Parse("12345678-1234-1234-1234-1234567890AB"));
            if (stock != null)
            {
                stock.RemainQuantity = 1;
                dbContext.SaveChanges();
            }
        }

        var handler = _serviceProvider.GetRequiredService<IBusinessHandler<CreateOrderInput, CreateOrderOutput>>();
        var input = new CreateOrderInput
        {
            UserId = "U123456",
            ProductId = Guid.Parse("12345678-1234-1234-1234-1234567890AB"),
            Quantity = 2, // Valid range (1-5), but > stock (1)
            Remark = "Over stock"
        };
        var userInfo = new UserInfo("U123456", "TestUser", 1);

        // Act
        var result = await handler.ExecuteAsync(input, userInfo, CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.Contains("库存不足") || e.Contains("Stock")); 
    }
    
    [Fact]
    public async Task CreateOrder_InvalidInput_ShouldFailValidation()
    {
        // Arrange
        var handler = _serviceProvider.GetRequiredService<IBusinessHandler<CreateOrderInput, CreateOrderOutput>>();
        var input = new CreateOrderInput
        {
            UserId = "", // Invalid: Required
            ProductId = Guid.Empty, 
            Quantity = 0, // Invalid: Range(1,5)
            Remark = "Invalid"
        };
        var userInfo = new UserInfo("U123456", "TestUser", 1);

        // Act
        var result = await handler.ExecuteAsync(input, userInfo, CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        // Expect validation errors
        Assert.NotEmpty(result.Errors);
    }
}
