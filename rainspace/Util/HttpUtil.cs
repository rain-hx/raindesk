using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace rainspace.Util
{
    public static class HttpUtil
    {
        private static readonly HttpClient Client = CreateClient();

        /// <summary>
        /// 同步 GET 请求（用于后台线程调用）
        /// </summary>
        public static string Get(string? url)
        {
            if (!TryCreateUri(url, out Uri? uri)) return "";

            try
            {
                using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, uri);
                request.Headers.UserAgent.ParseAdd("rainspace");
                using HttpResponseMessage response = Client.Send(request);
                response.EnsureSuccessStatusCode();
                return response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                LogUtil.WriteErrorLog(ex, "GET请求失败: " + url);
                return "";
            }
        }

        /// <summary>
        /// 异步 GET 请求（推荐用于可能在 UI 线程调用的场景）
        /// </summary>
        public static async Task<string> GetAsync(string? url)
        {
            if (!TryCreateUri(url, out Uri? uri)) return "";

            try
            {
                using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, uri);
                request.Headers.UserAgent.ParseAdd("rainspace");
                using HttpResponseMessage response = await Client.SendAsync(request).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                LogUtil.WriteErrorLog(ex, "GET异步请求失败: " + url);
                return "";
            }
        }

        /// <summary>
        /// 同步 POST 请求（用于后台线程调用）
        /// </summary>
        public static string Post(string? url, string? data)
        {
            if (!TryCreateUri(url, out Uri? uri)) return "";

            try
            {
                using StringContent content = new StringContent(data ?? "", Encoding.UTF8, "application/json");
                using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, uri)
                {
                    Content = content
                };
                request.Headers.UserAgent.ParseAdd("rainspace");
                using HttpResponseMessage response = Client.Send(request);
                response.EnsureSuccessStatusCode();
                return response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                LogUtil.WriteErrorLog(ex, "POST请求失败: " + url);
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
