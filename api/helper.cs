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
			code = (int)HttpStatusCode.BadRequest;
			guid = Guid.Empty;
			return false;
		}

		code = (int)HttpStatusCode.OK;
		guid = newGuid;
		return true;
	}

	public static byte[] Compress(byte[] b)
	{
		using (MemoryStream ms = new())
		{
			using (GZipStream gzip = new(ms, CompressionMode.Compress, true))
			{
				gzip.Write(b, 0, b.Length);
			}
			return ms.ToArray();
		}
	}

	public static T GetValueOrDefault<T>(this T[] array, int i, T @default = default)
	{
		if (array.Length <= i)
			return @default;
		return array[i];
	}
}