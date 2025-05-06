namespace ImageToAscii;

using System.Text;
using ImageToAscii.Helper;
using ImageToAscii.Picture;
using ImageToAscii.Server;

public static class Program
{
	public const int DefaultPattern = 0;

	public static void TestAscii()
	{
		const string ExampleResult = @".\example\cat.txt";
		const string ExamplePicture = @".\example\cat.jpg";

		Console.WriteLine($"> Running Test on {ExamplePicture} to {ExampleResult}");

		using FileStream image = new(ExamplePicture, FileMode.Open);
		Pattern pattern = ImageToAscii.PatternList[DefaultPattern];

		try
		{
			string ascii = ImageToAscii.Load(image, pattern);

			using FileStream imageResult = new(ExampleResult, FileMode.Create);
			byte[] asciiBytes = Encoding.UTF8.GetBytes(ascii);
			imageResult.Write(asciiBytes);
		}
		catch (Exception)
		{
			throw;
		}
	}

	private static AsciiMakerServer _HttpServer;
	public static AsciiMakerServer HttpServer => _HttpServer;

	public const int DefaultPort = 8000;

	public static int Main(string[] args)
	{
		Console.WriteLine("Running AsciiMaker server!");

		ImageToAscii.LoadPatterns();
		Console.WriteLine("> Loaded Ascii Patterns");

		switch (args.GetValueOrDefault(0, ""))
		{
			case "test":
				TestAscii();
				break;
			default:
				_HttpServer = new AsciiMakerServer(DefaultPort);
				Console.WriteLine($"> Initised http server on {DefaultPort}");
				_HttpServer.Start();

				break;
		}

		return 0;
	}
}
