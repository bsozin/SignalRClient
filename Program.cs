using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.CommandLineUtils;

namespace SignalRClient
{
    class Program
    {

        static CommandOption _optionUrl = null!;
        static CommandOption _optionClients = null!;
        static CommandOption _optionBunchSize = null!;
        static CommandOption _optionRelaxation = null!;
        static CommandOption _optionMonitoring = null!;
        static CommandOption _optionConnects = null!;

        const string DefaultUrl = "https://portaldev.st.dev/echo";
        const int DefaultClients = 100;
        const int DefaultBunchSize = 10;
        const int DefaultRelaxation = 1;
        const int DefaultMonitoring = 1000;
        const int DefaultConnects = 1;

        static void Main(string[] args)
        {
            CommandLineApplication app = new CommandLineApplication(true);
            app.FullName = "SignalR connections tester";
            app.HelpOption("-h|--h");
            _optionUrl = app.Option("-u|--u <HUB_URL>", $"The URL to SignalR hub ({DefaultUrl})", CommandOptionType.SingleValue);
            _optionClients = app.Option("-c|--c <CLIENTS_NUMBER>", $"How many connections to establish ({DefaultClients})", CommandOptionType.SingleValue);
            _optionBunchSize = app.Option("-b|--b <BUNCH_SIZE>", $"How many connections to establish at once ({DefaultBunchSize})", CommandOptionType.SingleValue);
            _optionRelaxation = app.Option("-r|--r <RELAX_TIME>", $"Delay between portions of connections in seconds ({DefaultRelaxation})", CommandOptionType.SingleValue);
            _optionMonitoring = app.Option("-m|--m <MONITORING_TIME>", $"Delay between status refreshing in milliseconds ({DefaultMonitoring})", CommandOptionType.SingleValue);
            _optionConnects = app.Option("-a|--a <CONNECTION_ATTEMPTS>", $"How many times to try to connect ({DefaultConnects})", CommandOptionType.SingleValue);
            app.OnExecute(Run);
            app.Execute(args);
        }

        private async static Task<int> Run()
        {
            string hubUrl = _optionUrl.HasValue() ? _optionUrl.Value() : DefaultUrl;
            int totalNumberOfClients = _optionClients.HasValue() ? int.Parse(_optionClients.Value()) : DefaultClients;
            int bunchSize = _optionBunchSize.HasValue() ? int.Parse(_optionBunchSize.Value()) : DefaultBunchSize;
            TimeSpan relaxation = TimeSpan.FromSeconds(_optionRelaxation.HasValue() ? int.Parse(_optionRelaxation.Value()) : DefaultRelaxation);
            TimeSpan monitoring = TimeSpan.FromMilliseconds(_optionMonitoring.HasValue() ? int.Parse(_optionMonitoring.Value()) : DefaultMonitoring);
            int connects = _optionConnects.HasValue() ? int.Parse(_optionConnects.Value()) : DefaultConnects;

            Console.WriteLine($"HUB_URL: {hubUrl}");
            Console.WriteLine($"CLIENTS_NUMBER: {totalNumberOfClients}");
            Console.WriteLine($"BUNCH_SIZE: {bunchSize}");
            Console.WriteLine($"RELAX_TIME: {relaxation}");
            Console.WriteLine($"MONITORING_TIME: {monitoring}");
            Console.WriteLine($"CONNECTION_ATTEMPTS: {connects}");
            Console.WriteLine();

            if (totalNumberOfClients < 1)
                throw new ArgumentOutOfRangeException($"totalNumberOfClients ({totalNumberOfClients}) must be greater 0");
            if (bunchSize < 1)
                throw new ArgumentOutOfRangeException($"bunchSize ({bunchSize}) must be greater 0");
            if (bunchSize > totalNumberOfClients)
                throw new ArgumentOutOfRangeException($"bunchSize ({bunchSize}) may not be greater than totalNumberOfClients ({totalNumberOfClients}).");

            var clients = Enumerable.Range(0, totalNumberOfClients).Select(i => new HubClient(hubUrl, connects)).ToArray();
            var monitor = new Monitor(clients) { CheckPeriod = monitoring };
            _ = monitor.Run().ConfigureAwait(false); // Just run and forget

            Console.WriteLine($"*** Start connections by {bunchSize} *** ");
            var startTime = DateTime.Now;
            for (int idx = 0; idx < totalNumberOfClients; idx += bunchSize) {
                var connectTasks = clients.Skip(idx).Take(bunchSize).Select(c => c.Connect());
                await Task.WhenAll(connectTasks).ConfigureAwait(false);
                await Task.Delay(relaxation).ConfigureAwait(false);
            }
            Console.WriteLine($"*** Connections established in {DateTime.Now - startTime}***");

            var connectException = clients.FirstOrDefault(c => c.ConnectExeption != null)?.ConnectExeption;
            var closeException = clients.FirstOrDefault(c => c.CloseExeption != null)?.CloseExeption;

            if (connectException != null)
                Console.WriteLine($"\n*** One of connect exceptions:\n{connectException}");

            if (closeException != null)
                Console.WriteLine($"\n*** One of close exceptions:\n{closeException}");

            await Task.Delay(-1);
            return 0;
        }

    }
}
