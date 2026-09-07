using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text.Json;

namespace WMS.API.Middlewares;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException ex)
        {
            await WriteError(context, HttpStatusCode.BadRequest, ex.Message, "VALIDATION_ERROR");
        }
        catch (KeyNotFoundException ex)
        {
            await WriteError(context, HttpStatusCode.NotFound, string.IsNullOrWhiteSpace(ex.Message) ? "Không tìm thấy tài nguyên yêu cầu." : ex.Message, "NOT_FOUND");
        }
        catch (InvalidOperationException ex)
        {
            await WriteError(context, HttpStatusCode.BadRequest, ex.Message, "INVALID_OPERATION");
        }
        catch (UnauthorizedAccessException ex)
        {
            await WriteError(context, HttpStatusCode.Forbidden, "Bạn không có quyền thực hiện thao tác này.", "FORBIDDEN");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            await WriteError(context, HttpStatusCode.InternalServerError, "Đã có lỗi xảy ra trên hệ thống. Vui lòng thử lại sau.", "INTERNAL_ERROR");
        }
    }

    private static async Task WriteError(HttpContext context, HttpStatusCode status, string message, string code)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)status;
        var result = JsonSerializer.Serialize(new
        {
            success = false,
            code,
            message,
            data = (object?)null,
            timestamp = DateTime.UtcNow
        });
        await context.Response.WriteAsync(result);
    }
}