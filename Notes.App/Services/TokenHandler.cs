using System.Net;
using System.Net.Http.Headers;


namespace Notes.App.Services;
public class TokenHandler : DelegatingHandler
{
    private readonly AuthService _auth;
    public TokenHandler(AuthService auth) { _auth = auth; }


    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await _auth.GetAccessTokenAsync();
        if (!string.IsNullOrWhiteSpace(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);


        var response = await base.SendAsync(request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            if (await _auth.RefreshAsync())
            {
                var retry = request.Clone();
                var newToken = await _auth.GetAccessTokenAsync();
                if (!string.IsNullOrWhiteSpace(newToken))
                    retry.Headers.Authorization = new AuthenticationHeaderValue("Bearer", newToken);
                response.Dispose();
                return await base.SendAsync(retry, cancellationToken);
            }
        }
        return response;
    }
}


static class HttpRequestMessageExtensions
{
    public static HttpRequestMessage Clone(this HttpRequestMessage req)
    {
        var clone = new HttpRequestMessage(req.Method, req.RequestUri);
        foreach (var h in req.Headers) clone.Headers.TryAddWithoutValidation(h.Key, h.Value);
        if (req.Content != null) clone.Content = new StreamContent(req.Content.ReadAsStream());
        return clone;
    }
}