using DevParadigm.Example.Entities;
using Microsoft.EntityFrameworkCore;

namespace DevParadigm.Example.DbContexts;

/// <summary>
/// 从库EF上下文（读操作）
/// </summary>
public class ReadDbContext : DbContext
{
    public ReadDbContext(DbContextOptions<ReadDbContext> options) : base(options) { }

    public DbSet<Order> Orders { get; set; }
    public DbSet<Stock> Stocks { get; set; }
    public DbSet<Merchant> Merchants { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // 复用主库模型配置
        base.OnModelCreating(modelBuilder);
    }
}