using System.Diagnostics;
using System.Text;
using System.Threading.Channels;

namespace LogComponent;

public class AsyncLogger : IAsyncLogger, IDisposable
{
	private readonly Channel<string> channel;
	private readonly Task worker;
	private readonly CancellationTokenSource cts = new();

	private readonly int BufferSize = 8192;
	private readonly int BatchSize = 10;
	private readonly int Interval = 200;
	private bool acceptingLogs = true;
	private DateTime currentFileDate;

	public string FileName { get; }
	public Func<DateTime> Clock { get; }

	public AsyncLogger(string? fileName = null, Func<DateTime>? clock = null)
	{
		FileName = fileName ?? "logs\\app-logs.txt";
		Clock = clock ?? (() => DateTime.Now);
		currentFileDate = Clock().Date;
		channel = Channel.CreateUnbounded<string>(new UnboundedChannelOptions
		{
			SingleReader = true,
			SingleWriter = false
		});
		worker = Task.Run(() => ProcessLogsAsync(cts.Token));
	}

	public void StopWithFlush()
	{
		acceptingLogs = false;
		channel.Writer.Complete();
		_ = worker.ContinueWith(t =>
		{
			if (t.IsFaulted)
			{
				Debug.WriteLine($"[Logger] Logging task failed: {t.Exception?.GetBaseException().Message}");
			}
		}, TaskScheduler.Default);
	}

	public void StopWithoutFlush()
	{
		cts.Cancel();
	}

	public void Write(string text)
	{
		if (!acceptingLogs)
		{
			return;
		}

		try
		{
			if (!channel.Writer.TryWrite(text))
			{
				Debug.WriteLine($"[Logger] Failed to enqueue log");
			}
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"[Logger] Failed to enqueue log: {ex.Message}");
		}
	}

	public void Dispose()
	{
		channel.Writer.TryComplete();
		cts.Cancel();
	}

	private async Task ProcessLogsAsync(CancellationToken token)
	{
		var filePath = GetLogFilePath(FileName, currentFileDate);
		var writer = new StreamWriter(filePath, append: true, encoding: Encoding.UTF8, bufferSize: BufferSize)
		{
			AutoFlush = false
		};

		var buffer = new List<string>(BatchSize);
		var flushInterval = TimeSpan.FromMilliseconds(Interval);
		var nextFlushTime = DateTime.UtcNow + flushInterval;

		try
		{
			await foreach (var log in channel.Reader.ReadAllAsync(token))
			{
				var now = Clock();

				if (now.Date > currentFileDate)
				{
					await FlushBufferAsync(writer, buffer);
					writer.Dispose();
					currentFileDate = now.Date;
					filePath = GetLogFilePath(FileName, currentFileDate);
					writer = new StreamWriter(filePath, append: true, encoding: Encoding.UTF8, bufferSize: BufferSize)
					{
						AutoFlush = false
					};
				}

				buffer.Add(log);

				if (buffer.Count >= BatchSize || DateTime.UtcNow >= nextFlushTime)
				{
					await FlushBufferAsync(writer, buffer);
					nextFlushTime = DateTime.UtcNow + flushInterval;
				}
			}
		}
		catch (OperationCanceledException)
		{
			Debug.WriteLine("[Logger] Logging task cancelled by request.");
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"[Logger] Background task error: {ex.Message}");
		}
		finally
		{
			await FlushBufferAsync(writer, buffer);
			await writer.FlushAsync();
			writer.Dispose();
		}
	}

	private static string GetLogFilePath(string baseFilePath, DateTime date)
	{
		string dir = Path.GetDirectoryName(baseFilePath) ?? ".";
		Directory.CreateDirectory(dir);
		string filenameWithoutExt = Path.GetFileNameWithoutExtension(baseFilePath);
		string ext = Path.GetExtension(baseFilePath);

		string datedName = $"{filenameWithoutExt}_{date:yyyy-MM-dd}{ext}";
		return Path.Combine(dir, datedName);
	}

	private static async Task FlushBufferAsync(StreamWriter writer, List<string> buffer)
	{
		if (buffer.Count == 0)
			return;

		foreach (var line in buffer)
		{
			await writer.WriteLineAsync(line);
		}

		buffer.Clear();
	}
}
