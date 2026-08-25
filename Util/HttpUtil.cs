using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace raindesk.Util
{
    public static class HttpUtil
    {
        private static readonly HttpClient Client = CreateClient();

        /// <summary>
        /// 异步 GET 请求（推荐用于可能在 UI 线程调用的场景）
        /// </summary>
        public static async Task<string> GetAsync(string? url, CancellationToken cancellationToken = default)
        {
            return await SendAsync(HttpMethod.Get, url, null, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// 异步 POST 请求
        /// </summary>
        public static async Task<string> PostAsync(string? url, string? data, CancellationToken cancellationToken = default)
        {
            using var content = new StringContent(data ?? "", Encoding.UTF8, "application/json");
            return await SendAsync(HttpMethod.Post, url, content, cancellationToken).ConfigureAwait(false);
        }

        private static async Task<string> SendAsync(
            HttpMethod method,
            string? url,
            HttpContent? content,
            CancellationToken cancellationToken)
        {
            if (!TryCreateUri(url, out Uri? uri)) return "";

            try
            {
                using HttpRequestMessage request = new HttpRequestMessage(method, uri)
                {
                    Content = content
                };
                request.Headers.UserAgent.ParseAdd("raindesk");
                using HttpResponseMessage response = await Client.SendAsync(request, cancellationToken).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return "";
            }
            catch (Exception ex)
            {
                LogUtil.WriteErrorLog(ex, $"{method.Method}异步请求失败: {url}");
                return "";
            }
        }

        private static HttpClient CreateClient()
        {
            return new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(12)
            };
        }

        private static bool TryCreateUri(string? url, out Uri? uri)
        {
            uri = null;
            return !string.IsNullOrWhiteSpace(url)
                && Uri.TryCreate(url, UriKind.Absolute, out uri)
                && uri.Scheme == Uri.UriSchemeHttps;
        }
    }
}
