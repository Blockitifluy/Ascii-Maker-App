namespace ImageToAscii.Helper;

public static class Helper
{

	public static T GetValueOrDefault<T>(this T[] array, int i, T @default)
	{
		if (array.Length <= i)
			return @default;
		return array[i];
	}
}