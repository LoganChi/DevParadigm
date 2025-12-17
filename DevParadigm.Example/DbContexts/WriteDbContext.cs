using DevParadigm.Example.Entities;
using Microsoft.EntityFrameworkCore;

namespace DevParadigm.Example.DbContexts;

/// <summary>
/// 主库EF上下文（写操作）
/// </summary>
public class WriteDbContext : DbContext
{
    public WriteDbContext(DbContextOptions<WriteDbContext> options) : base(options) { }

    public DbSet<Order> Orders { get; set; }
    public DbSet<Stock> Stocks { get; set; }
    public DbSet<Merchant> Merchants { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // 订单表配置
        modelBuilder.Entity<Order>(b =>
        {
            b.HasKey(o => o.Id);
            b.Property(o => o.OrderNo).IsRequired().HasMaxLength(32);
            b.Property(o => o.Amount).HasPrecision(18, 2);
        });

        // 库存表配置
        modelBuilder.Entity<Stock>(b =>
        {
            b.HasKey(s => s.ProductId);
            b.Property(s => s.RemainQuantity).IsRequired();
        });
    }
}