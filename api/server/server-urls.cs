namespace ImageToAscii.Server;

using System.ComponentModel;
using System.IO.Compression;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
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

public sealed class AsciiMakerServer : HTTPServer
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

		byte[] compressed = Helper.Compress(html);

		response.AddHeader("Content-Encoding", "gzip");
		response.ContentType = "text/html; charset=utf-8";
		response.ContentLength64 = compressed.Length;
		response.StatusCode = (int)HttpStatusCode.OK;
		response.OutputStream.Write(compressed, 0, compressed.Length);
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

			byte[] compressed = Helper.Compress(asset);

			response.ContentType = mimeType;
			response.ContentLength64 = compressed.Length;
			response.StatusCode = (int)HttpStatusCode.OK;
			response.AddHeader("Content-Encoding", "gzip");
			response.OutputStream.Write(compressed, 0, compressed.Length);
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

	// Can store 64 Megabytes
	const int MaxImageBufferSize = 1024 * 1024 * 64;

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
			response.StatusCode = (int)HttpStatusCode.UnsupportedMediaType;
			return;
		}

		Stream stream = request.InputStream;
		Guid guid = Guid.NewGuid();

		if (request.ContentLength64 <= 0)
		{
			response.StatusCode = (int)HttpStatusCode.LengthRequired;
			return;
		}
		else if (request.ContentLength64 > MaxImageBufferSize)
		{
			response.StatusCode = (int)HttpStatusCode.RequestEntityTooLarge;
			return;
		}

		byte[] b = new byte[request.ContentLength64];
		stream.Read(b);

		string guidString = guid.ToString();

		TempImage tempImage = new(b, mimeType);

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

		byte[] b = Helper.Compress(tempImage.Blob);

		response.ContentType = tempImage.MimeType;
		response.ContentLength64 = b.Length;
		response.StatusCode = (int)HttpStatusCode.OK;
		response.AddHeader("Content-Encoding", "gzip");
		response.OutputStream.Write(b);
	}

	public AsciiOptions GetAsciiOptions(HttpListenerRequest request)
	{
		byte[] bodyBuffer = new byte[request.ContentLength64];
		request.InputStream.Read(bodyBuffer, 0, bodyBuffer.Length);
		string rawJSON = Encoding.UTF8.GetString(bodyBuffer);

		var asciiOptions = JsonSerializer.Deserialize<AsciiOptions>(rawJSON);

		return asciiOptions;
	}

	/// <summary>
	/// HTTP: Handler converting an image into ascii art.
	/// </summary>
	/// <param name="context">HTTP context with a request of <c>?id=GUID&amp;size=int&amp;bright=float</c> and responses with ascii art.</param>
	[UrlHandler("/api/convert-image-to-ascii", ["POST"])]
	public void ConvertToAscii(HttpListenerContext context)
	{
		var response = context.Response;
		var request = context.Request;
		var query = request.QueryString;

		if (!Helper.TryToGetIDFromURL(query, out var code, out var guid))
		{
			response.StatusCode = code;
			return;
		}

		var tempImage = ImageCache.Get(guid);
		if (tempImage.Blob == null)
		{
			response.StatusCode = (int)HttpStatusCode.NotFound;
			return;
		}

		// TODO - More patterns / Custom pattern surport

		using MemoryStream stream = new(tempImage.Blob);

		stream.Position = 0;
		var asciiOptions = GetAsciiOptions(request);

		try
		{
			Pattern pattern = Program.DefaultPattern;

			Stream asciiStream = ImageToAscii.Load(stream, pattern, asciiOptions);
			asciiStream.Position = 0;

			byte[] b = new byte[asciiStream.Length];
			asciiStream.Read(b, 0, b.Length);

			byte[] compressed = Helper.Compress(b);

			response.AddHeader("Content-Encoding", "gzip");
			response.ContentLength64 = compressed.Length;
			response.OutputStream.Write(compressed, 0, compressed.Length);
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


		response.StatusCode = (int)HttpStatusCode.OK;
		response.ContentType = "text/plain; charset=utf-8";
	}

	public AsciiMakerServer(int port) : base(port) { }
}

#pragma warning restore CA1822 // Mark members as static