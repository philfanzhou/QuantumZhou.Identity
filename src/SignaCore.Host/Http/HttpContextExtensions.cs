using SignaCore.Database.Entity;

namespace SignaCore.Host.Http;

/// <summary>
/// The single implementation for reading cross-cutting values off <see cref="HttpContext"/>.
/// AdminController, AuthController and GatewayController each used to carry their own copy of these
/// methods, and they did not behave alike (see the comments on <see cref="GetClientIp"/>). A new
/// controller reuses these.
/// </summary>
public static class HttpContextExtensions
{
    /// <summary>
    /// The client IP: the head of the X-Forwarded-For chain when present, otherwise the remote
    /// address of the connection.
    /// <para>
    /// Note that a header that exists but is blank also falls back to the remote address. The old
    /// AuthController copy returned an empty string in that case, which made the audit records of
    /// one and the same client differ depending on which controller had served the request.
    /// </para>
    /// </summary>
    public static string? GetClientIp(this HttpContext context)
    {
        var forwarded = context.Request.Headers[IdentityHeaders.ForwardedFor].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(forwarded))
        {
            return forwarded.Split(',')[0].Trim();
        }

        return context.Connection.RemoteIpAddress?.ToString();
    }

    public static string? GetUserAgent(this HttpContext context) =>
        context.Request.Headers.UserAgent.ToString();

    /// <summary>
    /// The correlation id of this request. It reuses the single value the ServiceMantle correlation
    /// middleware already resolved and published to the response header and the logging scope —
    /// never re-reads the raw request header and never generates another id, so the value recorded
    /// in the audit table always matches the one in the logs and the response headers.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// The correlation middleware has not run for this request. Every production pipeline registers
    /// it first; reaching this branch means a caller bypassed the composed host.
    /// </exception>
    public static string GetCorrelationId(this HttpContext context) =>
        context.GetServiceMantleCorrelationId()
        ?? throw new InvalidOperationException(
            "The correlation id is unavailable because the ServiceMantle correlation middleware has not run for this request.");

    public static string? GetAppId(this HttpContext context) =>
        context.Items[IdentityHeaders.AppId] as string
        ?? context.Request.Headers[IdentityHeaders.AppId].FirstOrDefault();

    /// <summary>
    /// The redaction middleware has already moved the AppSecret into Items, so Items is read first
    /// and the request headers are only a fallback.
    /// </summary>
    public static string? GetAppSecret(this HttpContext context) =>
        context.Items[IdentityHeaders.AppSecret] as string
        ?? context.Request.Headers[IdentityHeaders.AppSecret].FirstOrDefault();

    public static AppRegistrationEntity? GetValidatedApp(this HttpContext context) =>
        context.Items[IdentityHeaders.ValidatedApp] as AppRegistrationEntity;
}
