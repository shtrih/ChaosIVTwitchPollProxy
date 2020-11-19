using System;
using System.IO;
using System.IO.Pipes;
using System.Threading.Tasks;
using System.Timers;

using Serilog;

namespace ChaosIVTwitchPollProxy
{
	class PipeClient
	{
		private readonly NamedPipeClientStream _pipe;
		private readonly StreamReader _pipeReader;
		private readonly StreamWriter _pipeWriter;

		private readonly Timer _pipeTimer = new Timer();
		public static readonly int PIPE_TIMER_INTERVAL = 200;

		private Task<string> _readPipeTask;

		public Action<string> OnMessage;

		private static ILogger _logger = Log.Logger.ForContext<PipeClient>();

		public PipeClient(string pipeName) {
			_pipeTimer.Interval = PIPE_TIMER_INTERVAL;
			_pipeTimer.Elapsed += OnElapsed;

			try {
				_pipe = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
				_pipe.Connect(10000);

				_pipeReader = new StreamReader(_pipe);
				_pipeWriter = new StreamWriter(_pipe);
				_pipeWriter.AutoFlush = true;

				_pipeTimer.Start();
			}
			catch (Exception ex) {
				_logger.Error(ex, $"Exception {ex.GetType().Name}: {ex.Message}");
				return;
			}
		}

		public void Send(string message) {
			_pipeWriter.WriteLine(message);
		}

		private void OnElapsed(object s, ElapsedEventArgs e) {
			try {
				if (_readPipeTask == null) {
					_readPipeTask = _pipeReader.ReadLineAsync();
				}
				else if (_readPipeTask.IsCompleted) {
					OnMessage?.Invoke(_readPipeTask.Result);
					_readPipeTask = null;
				}
			}
			catch (Exception ex) {
				_logger.Error(ex, $"Exception {ex.GetType().Name}: {ex.Message}");
				Disconnect();
			}
		}

		public bool IsConnected() {
			return _pipe.IsConnected;
		}

		public void Disconnect() {
			_pipeTimer.Close();
			_pipeReader.Close();
			_pipeWriter.Close();
			_pipe.Close();
		}
	}
}
