using System.Text.Json;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.PixelFormats;

namespace ImageToAscii.Picture;

using PixelType = HalfSingle;

static class ImageToAscii
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

	public const string PatternsPath = @"server/picture/patterns.json";

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

	public const string CharNotFound = "L";

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

	public static string Load(byte[] binImage, Pattern pattern)
	{
		if (!CanProcessImage(out var eCanProcess))
		{
			throw new Exception($"Can't process image {eCanProcess}");
		}

		using var image = Image.Load<PixelType>(binImage);

		string imageAscii = "";

		int area = (image.Height - 1) * (image.Width - 1);

		for (int i = 0; i <= area; i++)
		{
			int x = i % image.Width,
			y = i / image.Width;

			if (x == image.Width - 1)
				imageAscii += "\n";

			float raw = image[x, y].ToSingle();
			float pixel = (raw + 1f) / 2;

			string ascii = GetCharFromPattern(pixel, pattern);
			imageAscii += ascii + " ";

		}

		return imageAscii;
	}
}