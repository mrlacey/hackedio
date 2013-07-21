namespace Hacked.Extensions
{
    using System.Net.Http;
    using System.Threading.Tasks;

    public static class HttpClientExtensions
    {
        public static async Task<string> GetStringAsync(this HttpClient source, string format, params object[] args)
        {
            return await source.GetStringAsync(string.Format(format, args));
        }
    }
}