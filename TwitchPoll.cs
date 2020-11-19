using System;
using System.Collections.Generic;

using Fleck;

using Serilog;

namespace ChaosIVTwitchPollProxy
{
	public class TwitchPoll
	{
		private readonly int _port;
		public Action OnConnect;
		public Action OnDisconnect;
		public Action<string> OnMessage;
		private WebSocketServer _server;

		private List<Fleck.IWebSocketConnection> connections = new List<Fleck.IWebSocketConnection>();

		private static ILogger _logger = Log.Logger.ForContext<TwitchPoll>();

		public void StartServer() {
			var location = $"ws://127.0.0.1:{_port}";

			_logger.Information($"Waiting for Poll client connection ({location})...");

			_server = new Fleck.WebSocketServer(location);
			_server.RestartAfterListenError = false;

			_server.Start(socket => {
				socket.OnOpen += () => {
					connections.Add(socket);
					OnConnect?.Invoke();
					_logger.Information("FFZ Client connected.");
				};
				socket.OnClose += () => {
					connections.Remove(socket);
					OnDisconnect?.Invoke();
					_logger.Information("FFZ Client disconnected.");
				};
				socket.OnMessage += (string msg) => {
					OnMessage?.Invoke(msg);
				};
			});
		}

		private void Broadcast(string message) {
			connections.ForEach(connection => {
				if (connection.IsAvailable) { 
					connection.Send(message);
				}
				else {
					connection.Close();
				}
			});
		}

		public void Send(string message) {
			Broadcast(message);
		}

		public TwitchPoll(int port) {
			_port = port;

			StartServer();
		}

		public void Stop() {
			connections.ForEach(connection => {
				connection.Close();
				connections.Remove(connection);
			});
			_server.Dispose();
		}
	}
}
