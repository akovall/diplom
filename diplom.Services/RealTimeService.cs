using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace diplom.Services
{
    public sealed class RealTimeService
    {
        private static RealTimeService? _instance;
        private static readonly object _lock = new();

        public static RealTimeService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new RealTimeService(ApiClient.Instance);
                    }
                }

                return _instance;
            }
        }

        private readonly ApiClient _api;
        private HubConnection? _connection;
        private readonly SemaphoreSlim _startGate = new(1, 1);

        public event Action<int, int>? TimeEntryChanged; // (taskId, userId)
        public event Action<int>? TaskChanged; // (taskId)

        private RealTimeService(ApiClient api)
        {
            _api = api;
        }

        public async Task EnsureStartedAsync()
        {
            if (!_api.IsAuthenticated || string.IsNullOrWhiteSpace(_api.Token))
                return;

            await _startGate.WaitAsync();
            try
            {
                if (_connection != null && _connection.State != HubConnectionState.Disconnected)
                    return;

                var url = $"{_api.BaseUrl.TrimEnd('/')}/hubs/time";
                _connection = new HubConnectionBuilder()
                    .WithUrl(url, options =>
                    {
                        options.AccessTokenProvider = () => Task.FromResult(_api.Token)!;
                    })
                    .WithAutomaticReconnect()
                    .Build();

                _connection.On<int, int>("TimeEntryChanged", (taskId, userId) =>
                {
                    TimeEntryChanged?.Invoke(taskId, userId);
                });

                _connection.On<int>("TaskChanged", taskId =>
                {
                    TaskChanged?.Invoke(taskId);
                });

                await _connection.StartAsync();
            }
            finally
            {
                _startGate.Release();
            }
        }

        public async Task StopAsync()
        {
            await _startGate.WaitAsync();
            try
            {
                if (_connection == null)
                    return;

                try
                {
                    await _connection.StopAsync();
                }
                finally
                {
                    await _connection.DisposeAsync();
                    _connection = null;
                }
            }
            finally
            {
                _startGate.Release();
            }
        }
    }
}
