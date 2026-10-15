using Microsoft.AspNetCore.Http;
using WMS.API.Middlewares;
using Xunit;

namespace WMS.UnitTests.Security;

public class SecurityMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_InjectsExpectedSecurityHeaders()
    {
        var context = new DefaultHttpContext();
        var middleware = new SecurityHeadersMiddleware(next: (innerContext) => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        // Kích hoạt callback OnStarting của response
        // DefaultHttpContext không tự gọi OnStarting trừ khi stream bắt đầu hoặc ta kiểm tra trực tiếp
        // Trong unit test, gọi delegate callback hoặc ghi byte đầu tiên
        await context.Response.Body.WriteAsync(new byte[] { 1 }, 0, 1);

        var headers = context.Response.Headers;

        Assert.True(headers.ContainsKey("X-Content-Type-Options"));
        Assert.Equal("nosniff", headers["X-Content-Type-Options"]);

        Assert.True(headers.ContainsKey("X-Frame-Options"));
        Assert.Equal("DENY", headers["X-Frame-Options"]);

        Assert.True(headers.ContainsKey("Referrer-Policy"));
        Assert.Equal("strict-origin-when-cross-origin", headers["Referrer-Policy"]);

        Assert.True(headers.ContainsKey("X-XSS-Protection"));
        Assert.Equal("1; mode=block", headers["X-XSS-Protection"]);
    }
}