using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DevParadigm.Basement.Units;
using DevParadigm.Example.DTOs.Input;
using DevParadigm.Example.DTOs.Output;
using DevParadigm.Example.Entities;
using DevParadigm.Example.Extensions;
using DevParadigm.Interface;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DevParadigm.Test;

public class BatchOrderIntegrationTests : IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly SqliteConnection _connection;

    public BatchOrderIntegrationTests()
    {
        // Force load DevParadigm assembly for service scanning
        var _ = typeof(DevParadigm.Infrastructure.UnifiedGradedValidator);

        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        // Setup SQLite In-Memory
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        // Register services
        services.AddExampleLayer(configuration, (options, config) =>
        {
            options.UseSqlite(_connection);
        });

        _serviceProvider = services.BuildServiceProvider();
        
        // Ensure Database Created
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DevParadigm.Example.DbContexts.WriteDbContext>();
        dbContext.Database.EnsureDeleted();
        dbContext.Database.EnsureCreated();
        
        SeedData(dbContext);
    }

    private void SeedData(DbContext dbContext)
    {
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

        var productA = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var productB = Guid.Parse("22222222-2222-2222-2222-222222222222");

        if (dbContext.Set<Stock>().Find(productA) == null)
        {
            dbContext.Set<Stock>().AddRange(
                new Stock { ProductId = productA, RemainQuantity = 100, UpdateTime = DateTime.UtcNow },
                new Stock { ProductId = productB, RemainQuantity = 100, UpdateTime = DateTime.UtcNow }
            );
            dbContext.SaveChanges();
        }
    }

    [Fact]
    public async Task CreateBatch_DuplicateProducts_ShouldFailValidation()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<IBusinessHandler<CreateBatchOrderInput, CreateBatchOrderOutput>>();
        var productA = Guid.Parse("11111111-1111-1111-1111-111111111111");
        
        var input = new CreateBatchOrderInput
        {
            Orders = new List<CreateOrderInput>
            {
                new() { UserId = "U1", ProductId = productA, Quantity = 1 },
                new() { UserId = "U1", ProductId = productA, Quantity = 1 } // Duplicate ProductId
            }
        };

        // Act
        var result = await handler.ExecuteAsync(input, null, CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.Contains("Duplicate") || e.Contains("重复"));
    }

    [Fact]
    public async Task CreateBatch_ValidOrders_ShouldSuccess()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<IBusinessHandler<CreateBatchOrderInput, CreateBatchOrderOutput>>();
        var productA = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var productB = Guid.Parse("22222222-2222-2222-2222-222222222222");
        
        var input = new CreateBatchOrderInput
        {
            Orders = new List<CreateOrderInput>
            {
                new() { UserId = "U1", ProductId = productA, Quantity = 1 },
                new() { UserId = "U2", ProductId = productB, Quantity = 1 }
            }
        };

        // Act
        var result = await handler.ExecuteAsync(input, null, CancellationToken.None);

        // Assert
        Assert.True(result.Success, $"Batch handler failed: {string.Join(", ", result.Errors ?? new List<string>())}");
        Assert.NotNull(result.Data);
        Assert.Equal(2, result.Data.SuccessCount);
        Assert.Equal(0, result.Data.FailureCount);
    }

    public void Dispose()
    {
        _connection.Dispose();
    }
}
