// See https://aka.ms/new-console-template for more information
using DevParadigm.Basement.Units;
using DevParadigm.Console;
using DevParadigm.Example.DbContexts;
using DevParadigm.Example.DTOs.Input;
using DevParadigm.Example.DTOs.Output;
using DevParadigm.Interface;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SQLitePCL;

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json")
    .Build();

// 初始化 SQLitePCL
Batteries.Init(); // 添加这行
var services = new ServiceCollection();
services.AddApplicationServices(configuration);
services.AddSingleton<IConfiguration>(configuration);

var serviceProvider = services.BuildServiceProvider();
// 在 serviceProvider 构建后添加
using var scope = serviceProvider.CreateScope();
var writeDbContext = scope.ServiceProvider.GetRequiredService<WriteDbContext>();
await writeDbContext.Database.EnsureCreatedAsync();
// 2. 模拟测试数据
var createOrderInput = new CreateOrderInput
{
    UserId = "U123456",
    ProductId = Guid.Parse("12345678-1234-1234-1234-1234567890AB"),
    Quantity = 6,
    Remark = "测试订单备注，长度可能超出限制..."
};

var userInfo = new UserInfo("U123456", "测试用户", 1);

// 3. 获取业务处理器并执行
var orderHandler = serviceProvider.GetRequiredService<IBusinessHandler<CreateOrderInput, CreateOrderOutput>>();
var result = await orderHandler.ExecuteAsync(createOrderInput, userInfo, CancellationToken.None);

// 4. 输出结果
Console.WriteLine($"操作结果：{(result.Success ? "成功" : "失败")}");
Console.WriteLine($"消息：{result.Message}");

if (result.Success)
{
    Console.WriteLine($"订单ID：{result.Data?.OrderId}");
    Console.WriteLine($"订单编号：{result.Data?.OrderNo}");
    Console.WriteLine($"创建时间：{result.Data?.CreateTime}");
}
else
{
    Console.WriteLine("错误信息：");
    foreach (var error in result.Errors ?? new List<string>())
    {
        Console.WriteLine($"- {error}");
    }
}

Console.ReadLine();