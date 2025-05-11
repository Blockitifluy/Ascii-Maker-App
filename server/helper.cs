using System.Collections.Specialized;
using System.IO.Compression;
using System.Net;

namespace ImageToAscii.Helper;

public static class Helper
{
	public static bool TryToGetIDFromURL(NameValueCollection query, out int code, out Guid guid)
	{
		string id = query.Get("id");
		if (id == null)
		{
			code = (int)HttpStatusCode.BadRequest;
			guid = Guid.Empty;
			return false;
		}

		if (!Guid.TryParse(id, out var newGuid))
		{
			code = (int)HttpStatusCode.UnprocessableContent;
			guid = Guid.Empty;
			return false;
		}

		code = (int)HttpStatusCode.OK;
		guid = newGuid;
		return true;
	}

	public static byte[] Compress(byte[] input)
	{
		using var result = new MemoryStream();

		var lengthBytes = BitConverter.GetBytes(input.Length);
		result.Write(lengthBytes, 0, 4);

		using GZipStream gzipStream = new(result, CompressionMode.Compress);
		gzipStream.Write(input, 0, input.Length);
		gzipStream.Flush();
		return result.ToArray();
	}

	public static T GetValueOrDefault<T>(this T[] array, int i, T @default = default)
	{
		if (array.Length <= i)
			return @default;
		return array[i];
	}
}