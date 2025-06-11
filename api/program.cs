namespace ImageToAscii;

using System.Text;
using ImageToAscii.HelperClasses;
using ImageToAscii.Picture;
using ImageToAscii.Server;

public static class Program
{
	public const int DefaultPatternInt = 0;
	public static Pattern DefaultPattern => ImageToAscii.PatternList[DefaultPatternInt];

	/// <summary>
	/// Tests the ascii art system, uses <c>example/cat.jpg</c> and outputs to <c>example/cat.txt</c>
	/// </summary>
	public static void TestAscii()
	{
		const string ExampleResult = "example/cat.txt",
		ExamplePicture = "example/cat.jpg";

		Console.WriteLine($"> Running Test on {ExamplePicture} to {ExampleResult}");

		using FileStream image = new(ExamplePicture, FileMode.Open);
		Pattern pattern = DefaultPattern;
		image.Position = 0;

		try
		{
			using Stream asciiStream = ImageToAscii.Load(image, pattern);

			using FileStream imageResult = new(ExampleResult, FileMode.Create);
			asciiStream.CopyTo(imageResult);
		}
		catch (Exception ex)
		{
			LogSystem.Write(ex);
		}
	}

	/// <summary>
	/// The log file used for the server.
	/// </summary>
	private static AsciiServer _HttpServer;
	public static AsciiServer HttpServer => _HttpServer;

	public static string LogPath = "log/server.log";
	private static LogSystem _LogSystem;
	public static LogSystem LogSystem => _LogSystem;

	public const int DefaultPort = 8000;

	public static int Main(string[] args)
	{
		_LogSystem = new(LogPath);
		LogSystem.Clear();

		Console.ForegroundColor = ConsoleColor.Green;
		Console.WriteLine("\nRunning AsciiMaker server!");
		Console.ResetColor();

		ImageToAscii.LoadPatterns();
		Console.WriteLine("> Loaded Ascii Patterns");

		switch (args.GetValueOrDefault(0, ""))
		{
			case "test":
				TestAscii();
				break;
			case "":
				_HttpServer = new AsciiServer(DefaultPort);
				Console.WriteLine($"> Initised http server on {DefaultPort}");
				_HttpServer.Start();
				break;
			default:
				return 1;
		}

		return 0;
	}
}
