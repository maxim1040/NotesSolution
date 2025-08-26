// Services/TokenHandler.cs
using System.Net;
using System.Net.Http.Headers;

namespace Notes.App.Services;

public class TokenHandler : DelegatingHandler
{
    private readonly AuthService _auth;
    public TokenHandler(AuthService auth) => _auth = auth;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var token = await _auth.GetAccessTokenAsync();
        if (!string.IsNullOrWhiteSpace(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await BufferRequestContentAsync(request);

        var response = await base.SendAsync(request, ct);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            if (await _auth.RefreshAsync())
            {
                response.Dispose();
                var retry = await request.CloneAsync();
                var newToken = await _auth.GetAccessTokenAsync();
                if (!string.IsNullOrWhiteSpace(newToken))
                    retry.Headers.Authorization = new AuthenticationHeaderValue("Bearer", newToken);

                return await base.SendAsync(retry, ct);
            }
        }

        return response;
    }

    private static async Task BufferRequestContentAsync(HttpRequestMessage request)
    {
        if (request.Content is null) return;
        await request.Content.LoadIntoBufferAsync();
    }
}

static class HttpRequestMessageCloneExtensions
{
    public static async Task<HttpRequestMessage> CloneAsync(this HttpRequestMessage req)
    {
        var clone = new HttpRequestMessage(req.Method, req.RequestUri)
        {
            Version = req.Version,
#if NET8_0_OR_GREATER
            VersionPolicy = req.VersionPolicy,
#endif
        };

        
        foreach (var h in req.Headers)
            clone.Headers.TryAddWithoutValidation(h.Key, h.Value);

        if (req.Content != null)
        {
            var ms = new MemoryStream();
            await req.Content.CopyToAsync(ms);
            ms.Position = 0;

            var newContent = new StreamContent(ms);
            foreach (var h in req.Content.Headers)
                newContent.Headers.TryAddWithoutValidation(h.Key, h.Value);

            clone.Content = newContent;
        }

        return clone;
    }
}
