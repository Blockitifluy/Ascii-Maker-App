using System.Text;
using System.Text.Json;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace ImageToAscii.Picture;

using PixelType = HalfSingle;

public struct AsciiOptions
{
	public int Size { get; set; }
	public float Brightness { get; set; }
	public bool Invert { get; set; }

	public AsciiOptions(int size, float brightness, bool invert)
	{
		Size = size;
		Brightness = brightness;
		Invert = invert;
	}

	public AsciiOptions()
	{
		Size = 100;
		Brightness = 1.5f;
		Invert = false;
	}
}

[Serializable]
public class ProcessingException : Exception
{
	public ProcessingException() { }
	public ProcessingException(string message) : base(message) { }
	public ProcessingException(string message, Exception inner) : base(message, inner) { }
}

public static class ImageToAscii
{
	public static List<Pattern> PatternList = [];

	public enum ECanProcess
	{
		None,
		EmptyPattern
	}

	public static bool CanProcessImage(out ECanProcess eCanProcess)
	{
		if (PatternList.Count == 0)
		{
			eCanProcess = ECanProcess.EmptyPattern;
			return false;
		}

		eCanProcess = ECanProcess.EmptyPattern;
		return true;
	}

	public const string PatternsPath = @"api/picture/patterns.json";

	public static void LoadPatterns()
	{
		try
		{
			string patternsJson = File.ReadAllText(PatternsPath);

			var patterns = JsonSerializer.Deserialize<List<Pattern>>(patternsJson);

			PatternList = patterns; // Converts to List
		}
		catch (Exception)
		{
			throw;
		}
	}

	public const string CharNotFound = " ";

	public static string GetCharFromPattern(float grey, Pattern pattern)
	{
		PatternSet[] gradient = pattern.GetPatternSet();

		foreach (PatternSet select in gradient)
		{
			if (select.Position >= grey)
				return select.Char;
		}

		return CharNotFound;
	}

	private static string ProcessPixel(Image<PixelType> image, int i, Pattern pattern, AsciiOptions asciiOptions)
	{
		int x = i % image.Width,
			y = i / image.Width;

		string res = "";

		if (x == image.Width - 1)
			res = "\n";

		float raw = image[x, y].ToSingle();
		float pixel = (raw + 1f) / 2;
		if (asciiOptions.Invert)
			pixel = 1 - pixel;

		string ascii = GetCharFromPattern(pixel, pattern);
		res += ascii + " ";

		return res;
	}

	public static Stream Load(Stream ImageStream, Pattern pattern)
	{
		return Load(ImageStream, pattern, new());
	}

	public static Stream Load(Stream ImageStream, Pattern pattern, AsciiOptions asciiOptions)
	{
		if (!CanProcessImage(out var eCanProcess))
		{
			throw new ProcessingException($"Can't process image {eCanProcess}");
		}

		using var image = Image.Load<PixelType>(ImageStream);

		image.Mutate(i => i
			.Resize(asciiOptions.Size, 0)
			.Brightness(asciiOptions.Brightness)
		);

		MemoryStream asciiStream = new();

		int area = image.Height * image.Width - 1;

		for (int i = 0; i < area; i++)
		{
			string processed = ProcessPixel(image, i, pattern, asciiOptions);

			byte[] proBytes = Encoding.UTF8.GetBytes(processed);
			asciiStream.Write(proBytes);
		}

		return asciiStream;
	}
}