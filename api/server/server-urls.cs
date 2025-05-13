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

/// <summary>
/// A temporary image to be stored for the use of converting into ascii art.
/// </summary>
/// <param name="blob"><inheritdoc cref="TempImage.Blob" path="/summary"/></param>
/// <param name="mimeType"><inheritdoc cref="TempImage.MimeType" path="/summary"/></param>
public struct TempImage(byte[] blob, string mimeType)
{
	/// <summary>
	/// The image's bytes.
	/// </summary>
	public byte[] Blob = blob;
	/// <summary>
	/// The mime type of the image.
	/// </summary>
	public string MimeType = mimeType;
}

public partial class AsciiMakerServer
{
	/// <summary>
	/// HTTP: The root HTML of the website.
	/// </summary>
	/// <param name="context">HTTP context</param>
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

	/// <summary>
	/// HTTP: Handles all context of the folder <c>dist/assets</c>.
	/// </summary>
	/// <param name="context">HTTP context</param>
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

	/// <summary>
	/// The amount of time, a <see cref="ImageCache"/> element lasts for.
	/// </summary>
	public static TimeSpan TempImageTimeSpan = new(1, 0, 0);

	// Can store 4 Megabytes
	const int MaxImageBufferSize = 1024 * 1024 * 4;

	/// <summary>
	/// HTTP: Handles uploading and downloading images for the Ascii Maker service. 
	/// </summary>
	/// <param name="context">HTTP context</param>
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

	/// <summary>
	/// HTTP: Handles uploading images for the Ascii Maker service.
	/// </summary>
	/// <param name="context">HTTP context with a request of an image body and responses with a Guid of the image.</param>
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
			response.StatusCode = (int)HttpStatusCode.BadRequest;
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

	/// <summary>
	/// HTTP: Handles downloading images to the client.
	/// </summary>
	/// <param name="context">HTTP context with a request of <c>?id=Guid</c> and responses with an image body.</param>
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

	/// <summary>
	/// HTTP: Handler converting an image into ascii art.
	/// </summary>
	/// <param name="context">HTTP context with a request of <c>?id=GUID&amp;size=int&amp;bright=float</c> and responses with ascii art.</param>
	[UrlHandler("/api/convert-image-to-ascii", ["GET"])]
	public void ConvertToImage(HttpListenerContext context)
	{
		var response = context.Response;
		var request = context.Request;
		var query = request.QueryString;

		if (!Helper.TryToGetIDFromURL(query, out var code, out var guid))
		{
			response.StatusCode = code;
			return;
		}

		if (!int.TryParse(query.Get("size"), out var size))
		{
			response.StatusCode = (int)HttpStatusCode.BadRequest;
			return;
		}

		if (!float.TryParse(query.Get("bright"), out var brightness))
		{
			response.StatusCode = (int)HttpStatusCode.BadRequest;
			return;
		}

		var tempImage = ImageCache.Get(guid);
		if (tempImage.Blob == null)
		{
			response.StatusCode = (int)HttpStatusCode.NotFound;
			return;
		}

		// TODO - More patterns / Custom pattern surport

		string asciiImage;

		MemoryStream stream = new();
		stream.Write(tempImage.Blob);
		stream.Position = 0;

		AsciiOptions asciiOptions = new(size, brightness);

		try
		{
			Pattern pattern = ImageToAscii.PatternList[Program.DefaultPattern];
			asciiImage = ImageToAscii.Load(stream, pattern, asciiOptions);
		}
		catch (ImageFormatException)
		{
			response.StatusCode = (int)HttpStatusCode.UnsupportedMediaType;
			return;
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