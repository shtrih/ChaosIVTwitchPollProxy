using System;
using Serilog;

namespace ChaosIVTwitchPollProxy
{
	class Program
	{
		private static TwitchPoll _twitchPoll;
		private static PipeClient _pipeClient;

		private static ILogger _logger;

		static void Main(string[] args) {
			Log.Logger = new LoggerConfiguration()
			   .MinimumLevel.Debug()
			   .WriteTo.Console()
			   .WriteTo.File(
					"./ChaosIVTwitchPollProxy-.log", 
					outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] [{SourceContext:l}] {Message:lj}{NewLine}{Exception}", 
					rollingInterval: RollingInterval.Day,
					retainedFileCountLimit: 3
				)
			   .CreateLogger()
			;
			_logger = Log.Logger.ForContext<Program>();

			_logger.Information("Starting ChaosIVTwitchPollProxy");
			_logger.Information("Waiting pipe server to connect to");

			_pipeClient = new PipeClient("ChaosIVTwitchPollProxyPipe");
			_pipeClient.OnMessage += OnPipeMessage;

			var port = 8081;
			if (args.Length > 0) { 
				try {
					port = Int32.Parse(args[0]);
				}
				catch (FormatException) {
					_logger.Warning($"Unable to parse port: '{args[0]}'");
				}
			}

			_twitchPoll = new TwitchPoll(port);
			_twitchPoll.OnConnect += PollOnConnect;
			_twitchPoll.OnDisconnect += PollOnDisconnect;
			_twitchPoll.OnMessage += PollOnMessage;

			while (_pipeClient.IsConnected()) {
				System.Threading.Thread.Sleep(10);
			}

			_twitchPoll.Stop();

			_logger.Information("Pipe disconnected. Exiting...");
		}

		private static void PollOnMessage(string message) {
			_logger.Debug("From WS: " + message);
			_pipeClient.Send(message);
		}

		private static void PollOnConnect() {
			_logger.Debug("Connected");
			_pipeClient.Send("connected");
		}

		private static void PollOnDisconnect() {
			_logger.Debug("Disconnected");
			_pipeClient.Send("disconnected");
		}

		private static void OnPipeMessage(string message) {
			_logger.Debug("From Pipe: " + message);
			if (message?.Length > 0) {
				_twitchPoll.Send(message);
			}
		}
	}
}
