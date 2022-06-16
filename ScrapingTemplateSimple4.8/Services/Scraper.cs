using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ScrapingTemplateSimple4._8.Extensions;

namespace ScrapingTemplateSimple4._8.Services
{
    public class Scraper
    {
        private readonly HttpClient _client;
        private List<HttpClient> _clients;
        private int _idx;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private readonly int _threads;
        private bool _userProxies = false;

        public Scraper(int threads)
        {
            _threads = threads;
            _client = new HttpClient(new HttpClientHandler()
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            });
            if (File.Exists("proxies.txt"))
            {
                InitClientProxies();
                _clients.Add(_client); //add your ip to proxies
            }
        }

        private void InitClientProxies()
        {
            _clients = new List<HttpClient>();
            var proxies = File.ReadAllLines("proxies.txt");
            foreach (var p in proxies)
            {
                var pp = p.Split(':');
                var proxy = new WebProxy($"{pp[0]}:{pp[1]}", true)
                {
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(pp[2], pp[3]),
                };
                _clients.Add(new HttpClient(new HttpClientHandler()
                {
                    Proxy = proxy,
                    UseCookies = false,
                    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
                }));
            }
        }
        
        async Task<HttpClient> GetNextClient()
        {
            if (!_userProxies) return _client;
            await _semaphore.WaitAsync();
            var client = _clients[_idx];
            _idx++;
            if (_idx == _clients.Count)
                _idx = 0;
            _semaphore.Release();
            return client;
        }

        async Task<string> Work(string url)
        {
            var client = await GetNextClient();
            var doc = await client.GetHtml(url).ToDoc();
            return url;
        }

        public async Task MainWork(CancellationToken ct)
        {
            Notifier.Display("Started working");
            var urls = new List<string> { "a", "b" };
            var results = await urls.Parallel(_threads, Work);
            Notifier.Display("Completed working");
        }
    }
}