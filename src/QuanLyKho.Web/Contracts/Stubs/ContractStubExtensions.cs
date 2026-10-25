namespace QuanLyKho.Web.Contracts.Stubs;

public static class ContractStubExtensions
{
    /// <summary>
    /// Đăng ký bản giả của mọi dịch vụ trong Contracts. Gọi TRƯỚC các module Area trong Program.cs.
    /// Khi dịch vụ thật xong, nhóm sở hữu đăng ký bản thật trong module Area của mình
    /// (ví dụ <c>services.AddScoped&lt;IStockService, StockService&gt;()</c>); đăng ký sau cùng thắng nên bản thật tự thay bản giả.
    /// </summary>
    public static IServiceCollection AddContractStubs(this IServiceCollection services)
    {
        // Singleton vì giữ trạng thái trong bộ nhớ và không phụ thuộc dịch vụ nào khác.
        services.AddSingleton<IStockService, StockServiceStub>();
        services.AddSingleton<ILockService, LockServiceStub>();
        services.AddSingleton<IAuditService, AuditServiceStub>();
        services.AddSingleton<IHangHoaService, HangHoaServiceStub>();
        services.AddSingleton<INhapKhoService, NhapKhoServiceStub>();
        services.AddSingleton<IXuatKhoService, XuatKhoServiceStub>();

        // Scoped vì phụ thuộc dịch vụ khác hoặc IGoiYHandler (bản thật thường là scoped); trạng thái nằm ở store singleton.
        services.AddSingleton<GoiYStubStore>();
        services.AddScoped<IGoiYService, GoiYServiceStub>();
        services.AddScoped<ICapacityService, CapacityServiceStub>();
        return services;
    }
}
