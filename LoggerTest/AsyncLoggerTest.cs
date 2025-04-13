using LogComponent;
using static System.Net.Mime.MediaTypeNames;

namespace LoggerTest
{
	public class AsyncLoggerTest
	{
		[Fact]
		public async Task When_Call_ILogger_Then_Log_Created()
		{
			// Arrange
			var directory = Path.Combine(AppContext.BaseDirectory, "logs");
			if (Directory.Exists(directory))
				Directory.Delete(directory, true);

			var filePath = Path.Combine(directory ,"test-logs.txt");
			IAsyncLogger logger = new AsyncLogger(filePath, () => DateTime.Parse("2000/01/01"));

			// Act
			logger.Write("Test Log Entry 1");
			logger.Write("Test Log Entry 2");

			logger.StopWithFlush();

			await Task.Delay(200);

			// Assert
			var files = Directory.GetFiles(directory);
			Assert.Single(files);

			var lines = File.ReadAllLines(files[0]);
			Assert.Equal(2, lines.Length);

			var fileName = Path.GetFileName(files[0]);
			Assert.Equal("test-logs_2000-01-01.txt", fileName);
		}

		[Fact]
		public async Task When_After_Midnight_Then_New_File_Created()
		{
			// Arrange
			var directory = Path.Combine(AppContext.BaseDirectory, "logs");
			if (Directory.Exists(directory))
				Directory.Delete(directory, true);

			var filePath = Path.Combine(directory, "test-logs.txt");
			var date = DateTime.Parse("2000-01-01T23:59:59.999");
			DateTime clock() => date;
			IAsyncLogger logger = new AsyncLogger(filePath, clock);

			// Act
			logger.Write("Test Log Entry 1");
			await Task.Delay(1000);
			date = date.AddSeconds(1);
			logger.Write("Test Log Entry 2");

			logger.StopWithFlush();

			await Task.Delay(200);

			// Assert
			var files = Directory.GetFiles(directory);
			Assert.Equal(2, files.Length);
		}

		[Fact]
		public async Task When_StopWithoutFlush_Called_Then_Outstanding_Logs_Not_Written()
		{
			// Arrange
			var directory = Path.Combine(AppContext.BaseDirectory, "logs");
			if (Directory.Exists(directory))
				Directory.Delete(directory, true);

			IAsyncLogger logger = new AsyncLogger();

			// Act
			logger.Write("Test Log Entry 1");
			logger.Write("Test Log Entry 2");
			logger.StopWithoutFlush();
			await Task.Delay(200);

			// Assert
			var files = Directory.GetFiles(directory);
			Assert.Single(files);

			var lines = File.ReadAllLines(files[0]);
			Assert.Empty(lines);
		}
	}
}