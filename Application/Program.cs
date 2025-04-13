using LogComponent;

IAsyncLogger logger = new AsyncLogger();
for (int i = 0; i < 15; i++)
{
	logger.Write("Number with Flush: " + i.ToString());
}
logger.StopWithFlush();

IAsyncLogger logger2 = new AsyncLogger("logs\\web-logs.txt");
for (int i = 0; i < 25; i++)
{
	logger2.Write("Number without Flush: " + i.ToString());
}
await Task.Delay(200);
logger2.StopWithoutFlush();

Console.WriteLine("Logging finished. Press Enter to exit.");
Console.ReadLine();