namespace ImageToAscii.Server;
using System.Net;
using System.Web;
using MimeTypes;

#pragma warning disable CA1822 // Mark members as static

public partial class AsciiMakerServer
{
	[UrlHandler("/")]
	public void Root(HttpListenerContext context, HttpListenerResponse response)
	{
		const string HTMLPath = @"dist\index.html";
		byte[] html = File.ReadAllBytes(HTMLPath);

		response.ContentType = "text/html; charset=utf-8";
		response.ContentLength64 = html.Length;
		response.StatusCode = (int)HttpStatusCode.OK;
		response.OutputStream.Write(html, 0, html.Length);
	}

	[UrlHandler("/assets/~")]
	public void Assets(HttpListenerContext context, HttpListenerResponse response)
	{
		try
		{
			string assetName = context.Request.Url.Segments[2];
			string assetPath = Path.Combine(@"dist\assets", assetName);
			byte[] asset = File.ReadAllBytes(assetPath);

			string mimeType = MimeTypeMap.GetMimeType(Path.GetExtension(assetName));
			if (string.IsNullOrEmpty(mimeType))
			{
				mimeType = "application/octet-stream"; // Default to binary if mime type is not found
			}

			response.ContentType = mimeType;
			response.ContentLength64 = asset.Length;
			response.StatusCode = (int)HttpStatusCode.OK;
			response.OutputStream.Write(asset, 0, asset.Length);
		}
		catch (FileNotFoundException ex)
		{
			Console.WriteLine($"File not found: {ex.Message}");
			response.StatusCode = (int)HttpStatusCode.NotFound;
		}
	}
}

#pragma warning restore CA1822 // Mark members as static