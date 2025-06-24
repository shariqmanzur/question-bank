using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;

namespace QuestionBank.Utils
{
    public static class MathConverter
    {
        private static readonly HttpClient _http = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(15)
        };

        // 1 hr sliding expiration, size limit just to guard against runaway growth
        private static readonly MemoryCache _cache = new MemoryCache(
            new MemoryCacheOptions { SizeLimit = 1024 }
        );

        public static async Task<string> ToSvgDataUriAsync(string tex)
        {
            if (string.IsNullOrWhiteSpace(tex))
                return string.Empty;

            // Check cache
            if (_cache.TryGetValue(tex, out string uri))
                return uri;

            // Fetch from codecogs
            var encoded = WebUtility.UrlEncode(tex);
            var url = $"https://latex.codecogs.com/svg.latex?{encoded}";
            var response = await _http.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var svgBytes = await response.Content.ReadAsByteArrayAsync();
            var base64 = Convert.ToBase64String(svgBytes);
            uri = $"data:image/svg+xml;base64,{base64}";

            // Cache it (size=1 per entry)
            _cache.Set(
                tex,
                uri,
                new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(TimeSpan.FromHours(1))
                    .SetSize(1)
            );

            return uri;
        }
    }
}