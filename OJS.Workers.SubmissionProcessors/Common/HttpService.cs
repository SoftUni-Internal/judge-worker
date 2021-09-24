namespace OJS.Workers.SubmissionProcessors.Common
{
    using System;
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
            var content = this.PostJsonAsync(url, body).GetAwaiter().GetResult();

            return JsonConvert.DeserializeObject<TResponseBody>(content);
        }

        public async Task<string> PostJsonAsync<TRequestBody>(string url, TRequestBody body)
        {
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri(url),
                Content = new StringContent(
                    JsonConvert.SerializeObject(body),
                    Encoding.UTF8,
                    "application/json")
            };

            var response = await this.httpClient.SendAsync(request);
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