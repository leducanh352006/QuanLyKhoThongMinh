using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using QuanLyKho.Web.Contracts;

namespace QuanLyKho.Web.Areas.HeThong.Services;

/// <summary>Yêu cầu "vai trò của người dùng có quyền này trên chứng từ này" (sinh từ <see cref="CanQuyenAttribute"/>).</summary>
public sealed record CanQuyenRequirement(LoaiChungTu Loai, QuyenThaoTac Quyen) : IAuthorizationRequirement;

/// <summary>Đối chiếu yêu cầu với bảng PhanQuyen qua <see cref="IPhanQuyenService"/> (có nhớ tạm).</summary>
public sealed class CanQuyenHandler(IPhanQuyenService phanQuyen) : AuthorizationHandler<CanQuyenRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, CanQuyenRequirement requirement)
    {
        if (!int.TryParse(context.User.FindFirst(ClaimKho.MaVaiTro)?.Value, out var maVaiTro)) return;   // không thành công = bị từ chối
        if (await phanQuyen.CoQuyen(maVaiTro, requirement.Loai, requirement.Quyen))
            context.Succeed(requirement);
    }
}

/// <summary>Dựng chính sách <c>quyen:XK:Duyet</c> khi cần nên không phải đăng ký 20 chính sách; tên khác thì dùng cách mặc định.</summary>
public sealed class CanQuyenPolicyProvider(IOptions<AuthorizationOptions> options) : DefaultAuthorizationPolicyProvider(options)
{
    public override Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (ChinhSachQuyen.TryParse(policyName, out var loai, out var quyen))
        {
            var chinhSach = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new CanQuyenRequirement(loai, quyen))
                .Build();
            return Task.FromResult<AuthorizationPolicy?>(chinhSach);
        }
        return base.GetPolicyAsync(policyName);
    }
}
