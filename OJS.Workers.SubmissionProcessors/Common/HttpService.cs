namespace OJS.Workers.SubmissionProcessors.Common
{
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;
    using Newtonsoft.Json;

    public class HttpService
    {
        private readonly HttpClient httpClient;

        public HttpService()
            => this.httpClient = new HttpClient();

        public TResponseBody PostJson<TRequestBody, TResponseBody>(string url, TRequestBody body)
        {
            var content = this.PostJsonAsync(url, body).Result;

            return JsonConvert.DeserializeObject<TResponseBody>(content);
        }

        public async Task<string> PostJsonAsync<TRequestBody>(string url, TRequestBody body)
        {
            var httpContent = new StringContent(
                JsonConvert.SerializeObject(body),
                Encoding.UTF8,
                "application/json");

            // ConfigureAwait(false) necessary to prevent deadlocks and return response to current thread
            var response = await this.httpClient.PostAsync(url, httpContent).ConfigureAwait(false);

            var content = await response.Content.ReadAsStringAsync();

            return content;
        }

        public string Get(string url)
            => this.httpClient.GetAsync(url)
                .Result
                .Content
                .ReadAsStringAsync()
                .Result;

        public TResponse Get<TResponse>(string url)
            => JsonConvert.DeserializeObject<TResponse>(this.Get(url));
    }
}