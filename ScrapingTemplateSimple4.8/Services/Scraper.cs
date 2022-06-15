using ScrapingTemplateSimple4._8.Models;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace ScrapingTemplateSimple4._8.Services
{
    public class Scraper
    {
        private HttpClient _client;
        private int _threads;
        public Scraper(int threads)
        {
            _threads = threads;
            _client = new HttpClient(new HttpClientHandler()
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            });
        }

        public async Task MainWork(CancellationToken ct)
        {
            Notifier.Display("Started working");


            Notifier.Display("Completed working");
        }
    }
}