namespace ImageToAscii.Server;

using System.IO.Compression;
using System.Net;
using System.Text;
using System.Web;
using ImageToAscii.Helper;
using ImageToAscii.Picture;
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

	// Can store 4 Megabytes
	const int MaxImageBufferSize = 1024 * 1024 * 4;

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


		string mimeType = request.Headers.Get("Content-Type");
		if (mimeType == null)
		{
			response.StatusCode = (int)HttpStatusCode.BadRequest;
			return;
		}

		Stream stream = request.InputStream;
		Guid guid = Guid.NewGuid();

		if (request.ContentLength64 <= 0)
		{
			response.StatusCode = (int)HttpStatusCode.UnprocessableContent;
			return;
		}

		if (request.ContentLength64 > MaxImageBufferSize)
		{
			response.StatusCode = (int)HttpStatusCode.RequestEntityTooLarge;
			return;
		}

		byte[] buffer = new byte[MaxImageBufferSize];
		stream.Read(buffer, 0, buffer.Length);

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

		if (!Helper.TryToGetIDFromURL(request.QueryString, out var code, out var guid))
		{
			response.StatusCode = code;
			return;
		}

		var tempImage = ImageCache.Get(guid);
		if (tempImage.Blob.Length <= 0)
		{
			response.StatusCode = (int)HttpStatusCode.NotFound;
			return;
		}

		response.ContentType = tempImage.MimeType;
		response.ContentLength64 = tempImage.Blob.Length;
		response.StatusCode = (int)HttpStatusCode.OK;
		response.OutputStream.Write(tempImage.Blob, 0, tempImage.Blob.Length);
	}

	[UrlHandler("/api/convert-image-to-ascii", ["GET"])]
	public void ConvertToImage(HttpListenerContext context)
	{
		var response = context.Response;
		var request = context.Request;

		if (!Helper.TryToGetIDFromURL(request.QueryString, out var code, out var guid))
		{
			response.StatusCode = code;
			return;
		}

		var tempImage = ImageCache.Get(guid);
		if (tempImage.Blob.Length <= 0)
		{
			response.StatusCode = (int)HttpStatusCode.NotFound;
			return;
		}

		// TODO - More patterns / Custom pattern surport

		string asciiImage;

		try
		{
			Pattern pattern = ImageToAscii.PatternList[0];
			asciiImage = ImageToAscii.Load(tempImage.Blob, pattern);
		}
		catch (Exception)
		{
			throw;
		}

		byte[] asciiBytes = Encoding.UTF8.GetBytes(asciiImage);

		response.StatusCode = (int)HttpStatusCode.OK;
		response.ContentLength64 = asciiBytes.Length;
		response.ContentType = "text/plain; charset=utf-8";
		response.OutputStream.Write(asciiBytes);
	}
}

#pragma warning restore CA1822 // Mark members as static