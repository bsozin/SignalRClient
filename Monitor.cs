using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SignalRClient
{
    public class Monitor
    {
        private HubClient[] _clients = null!;
        private static HubClient.ConnectionStatus[] _states = Enum.GetValues<HubClient.ConnectionStatus>();

        public TimeSpan CheckPeriod { get; set; } = TimeSpan.FromSeconds(1);

        public Monitor(HubClient[] clients)
        {
            _clients = clients;
        }

        public async Task Run(CancellationToken ct = default)
        {
            var prevSummary = CalcStateSummary(true);

            Log($"*** Start monitoring {_clients.Length} clients ***");
            while (!ct.IsCancellationRequested) {
                var newSummary = CalcStateSummary();
                if (AreDifferent(prevSummary, newSummary)) {
                    prevSummary = newSummary;
                    Log(SummaryToString(newSummary));
                }
                await Task.Delay(CheckPeriod, ct).ContinueWith(tsk => { }).ConfigureAwait(false);
            }
            Log(SummaryToString(CalcStateSummary()));
            Log($"*** Done monitoring clients ***");
        }

        private int[] CalcStateSummary(bool returnEmpty = false)
        {
            int[] summary = new int[_states.Length];
            if (!returnEmpty) {
                foreach (var client in _clients) {
                    summary[(int)client.Status]++;
                }
            }
            return summary;
        }

        private string SummaryToString(int[] summary)
        {
            StringBuilder sb = new StringBuilder(DateTime.Now.ToLongTimeString());
            foreach (var state in _states) {
                sb.Append($" {state}={summary[(int)state]}");
            }
            return sb.ToString();
        }

        private bool AreDifferent(int[] a, int[] b)
        {
            for (int idx = 0; idx < a.Length; idx++)
                if (a[idx] != b[idx])
                    return true;
            return false;
        }

        private void Log(string msg)
        {
            Console.WriteLine(msg);
        }
    }
}
