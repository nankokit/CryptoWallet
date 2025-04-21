using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace CryptoWallet
{
    public class LoggingHttpMessageHandler : DelegatingHandler
    {
        private readonly string _logFilePath = "rpc_log.txt";

        public LoggingHttpMessageHandler(HttpMessageHandler innerHandler = null)
            : base(innerHandler ?? new HttpClientHandler())
        {
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            try
            {
                var requestContent = request.Content != null ? await request.Content.ReadAsStringAsync() : "";
                var requestLog = $"[{DateTime.Now}] Request: {request.Method} {request.RequestUri}\n{requestContent}\n";

                await File.AppendAllTextAsync(_logFilePath, requestLog, cancellationToken);

                var response = await base.SendAsync(request, cancellationToken);

                var responseContent = await response.Content.ReadAsStringAsync();
                var responseLog = $"[{DateTime.Now}] Response: {response.StatusCode}\n{responseContent}\n";

                await File.AppendAllTextAsync(_logFilePath, responseLog, cancellationToken);

                return response;
            }
            catch (IOException ex)
            {
                await Console.Error.WriteLineAsync($"Error writing to log file: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                await Console.Error.WriteLineAsync($"Unexpected error in LoggingHttpMessageHandler: {ex.Message}");
                throw;
            }
        }
    }
}