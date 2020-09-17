namespace OJS.Workers.SubmissionProcessors.Common
{
    using System;
    using System.Net.Http;
    using System.Text;

    using Newtonsoft.Json;

    public class HttpService
    {
        private readonly HttpClient httpClient;

        public HttpService()
            => this.httpClient = new HttpClient();

        public TResponseBody PostJson<TRequestBody, TResponseBody>(string url, TRequestBody body)
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

            var response = this.httpClient.SendAsync(request)
                .Result;
            var content = response.Content.ReadAsStringAsync()
                .Result;

            return JsonConvert.DeserializeObject<TResponseBody>(content);
        }
    }
}