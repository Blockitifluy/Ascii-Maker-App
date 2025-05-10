namespace ImageToAscii.Server;
using System.Net;
using System.Text;
using System.Web;
using ImageToAscii.Helper;
using MimeTypes;
using SixLabors.ImageSharp;

#pragma warning disable CA1822 // Mark members as static

public struct TempImage(byte[] blob, string mimeType)
{
	public byte[] Blob = blob;
	public string MimeType = mimeType;
}

public partial class AsciiMakerServer
{
	[UrlHandler("/", ["GET"])]
	public void Root(HttpListenerContext context)
	{
		const string HTMLPath = @"dist\index.html";
		byte[] html = File.ReadAllBytes(HTMLPath);

		var response = context.Response;

		response.ContentType = "text/html; charset=utf-8";
		response.ContentLength64 = html.Length;
		response.StatusCode = (int)HttpStatusCode.OK;
		response.OutputStream.Write(html, 0, html.Length);
	}

	[UrlHandler("/assets/~", ["GET"])]
	public void Assets(HttpListenerContext context)
	{
		var response = context.Response;
		string assetName = context.Request.Url.Segments[2];
		string assetPath = Path.Combine(@"dist\assets", assetName);

		try
		{
			byte[] asset = File.ReadAllBytes(assetPath);

			string mimeType = MimeTypeMap.GetMimeType(Path.GetExtension(assetName));

			// Default to binary, if mime type is not found
			if (string.IsNullOrEmpty(mimeType))
			{
				mimeType = "application/octet-stream";
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

	public Cache<Guid, TempImage> ImageCache = new();

	public static TimeSpan TempImageTimeSpan = new(1, 0, 0);

	[UrlHandler("/api/image", ["POST", "GET"])]
	public void HandleImage(HttpListenerContext context)
	{
		string method = context.Request.HttpMethod;
		switch (method)
		{
			case "POST":
				UploadImage(context);
				break;
			case "GET":
				GetImage(context);
				break;
			default:
				context.Response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
				break;
		}
	}

	public void UploadImage(HttpListenerContext context)
	{
		var response = context.Response;
		var request = context.Request;

		Guid guid = Guid.NewGuid();
		Stream imageStream = request.InputStream;

		string mimeType = request.Headers.Get("Content-Type");
		if (mimeType == null)
		{
			response.StatusCode = (int)HttpStatusCode.BadRequest;
			return;
		}

		// TODO - Add async
		byte[] buffer = Array.Empty<byte>();
		imageStream.Read(buffer, 0, buffer.Length);

		string guidString = guid.ToString();

		TempImage tempImage = new(buffer, mimeType);

		ImageCache.Store(guid, tempImage, TempImageTimeSpan);

		byte[] guidBytes = Encoding.UTF8.GetBytes(guidString);

		response.ContentType = "text/plain; charset=utf-8";
		response.ContentLength64 = guidBytes.Length;
		response.StatusCode = (int)HttpStatusCode.Accepted;
		response.OutputStream.Write(guidBytes, 0, guidBytes.Length);
	}

	public void GetImage(HttpListenerContext context)
	{
		var response = context.Response;
		var request = context.Request;

		string id = request.QueryString.Get("id");
		if (id == null)
		{
			response.StatusCode = (int)HttpStatusCode.BadRequest;
			return;
		}

		Guid guid = Guid.Parse(id);
		var tempImage = ImageCache.Get(guid);

		response.ContentType = tempImage.MimeType;
		response.ContentLength64 = tempImage.Blob.Length;
		response.StatusCode = (int)HttpStatusCode.OK;
		response.OutputStream.Write(tempImage.Blob, 0, tempImage.Blob.Length);
	}
}

#pragma warning restore CA1822 // Mark members as static