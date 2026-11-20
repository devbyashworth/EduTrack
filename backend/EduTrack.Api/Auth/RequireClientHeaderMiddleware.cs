namespace EduTrack.Api.Auth;

/// <summary>
/// Cross-site request forgery defence for cookie authentication. Every state-changing API request must carry a
/// custom header. A malicious page on another site cannot add custom headers to a request without triggering a
/// CORS preflight, which this API only approves for the configured frontend origin. Combined with SameSite
/// cookies, this blocks forged requests without needing per-form anti-forgery tokens.
/// </summary>
public sealed class RequireClientHeaderMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Requested-With";
    public const string HeaderValue = "EduTrack";

    public Task InvokeAsync(HttpContext context)
    {
        var request = context.Request;
        var changesState = !(HttpMethods.IsGet(request.Method)
                             || HttpMethods.IsHead(request.Method)
                             || HttpMethods.IsOptions(request.Method));

        if (changesState
            && request.Path.StartsWithSegments("/api")
            && request.Headers[HeaderName] != HeaderValue)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return context.Response.WriteAsJsonAsync(new { message = "Missing required request header." });
        }

        return next(context);
    }
}
