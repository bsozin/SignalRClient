using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;

namespace SignalRClient
{
    public class HubClient : IAsyncDisposable
    {
        public enum ConnectionStatus
        {
            Pending,
            Connecting,
            Retry,
            Connected,
            Disconnected,
            DisconnectedByError,
            ConnectionError
        }

        private HubConnection _connection = null!;

        private int _connectAttempts;

        public HubClient(string serverUrl, int connectAttempts = 0)
        {
            _connectAttempts = connectAttempts;
            _connection = new HubConnectionBuilder()
                .WithUrl(serverUrl, options => options.Transports = HttpTransportType.WebSockets)
                .Build();
            _connection.ServerTimeout = TimeSpan.FromMinutes(1);
            _connection.Closed += Connection_Closed;
            _connection.Reconnected += Connection_Reconnected;
            _connection.Reconnecting += Connection_Reconnecting;
        }

        public async Task Connect(CancellationToken ct = default)
        {
            if (_connection.State != HubConnectionState.Disconnected)
                throw new ApplicationException("Connection is in progress or has already been established");
            do {
                try {
                    if(Status != ConnectionStatus.Retry)
                        Status = ConnectionStatus.Connecting;
                    await _connection.StartAsync(ct).ConfigureAwait(false);
                    Status = ConnectionStatus.Connected;
                    _connectAttempts = 0;
                    LogMessage("Connected");
                }
                catch (Exception ex) {
                    ConnectExeption = ex;
                    Status = _connectAttempts > 1? ConnectionStatus.Retry : ConnectionStatus.ConnectionError;
                    LogMessage($"Connection error: {ex}");
                }
            } while (--_connectAttempts > 0);
        }

        public ConnectionStatus Status { get; private set; } = ConnectionStatus.Pending;

        public Exception? ConnectExeption { get; private set; }
        public Exception? CloseExeption { get; private set; }
        public Exception? ReconnectExeption { get; private set; }

        public async ValueTask DisposeAsync()
        {
            if (_connection != null) {
                await _connection.DisposeAsync().ConfigureAwait(false);
                _connection = null!;
            }
            GC.SuppressFinalize(this);
        }

        private Task Connection_Closed(Exception ex)
        {
            CloseExeption = ex;
            Status = ex == null ? ConnectionStatus.Disconnected : ConnectionStatus.DisconnectedByError;
            LogMessage($"Closed:{ex}");
            return Task.CompletedTask;
        }

        private Task Connection_Reconnecting(Exception ex)
        {
            ReconnectExeption = ex;
            LogMessage($"Reconnecting:{ex}");
            return Task.CompletedTask;
        }

        private Task Connection_Reconnected(string newConnectionId)
        {
            LogMessage($"Reconnected");
            return Task.CompletedTask;
        }

        private void LogMessage(string msg)
        {
            //Console.WriteLine($"{_connection?.ConnectionId}: {msg}");
        }

    }
}
