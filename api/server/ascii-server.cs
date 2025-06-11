namespace ImageToAscii.Server;

using System.ComponentModel;
using System.IO.Compression;
using System.Net;
using System.Reflection.Metadata;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Web;
using ImageToAscii.HelperClasses;
using ImageToAscii.Picture;
using MimeTypes;
using SixLabors.ImageSharp;

#pragma warning disable CA1822 // Mark members as static

public interface IAssetFile
{
	/// <summary>
	/// The image's bytes.
	/// </summary>
	public byte[] Blob { get; set; }
	public string Hash { get; set; }
}

public struct AssetFile : IAssetFile
{
	public byte[] Blob { get; set; }
	public string Hash { get; set; }

	public AssetFile(byte[] blob)
	{
		Blob = blob;

		SHA1 sha1 = SHA1.Create();
		Hash = '"' + Convert.ToHexString(sha1.ComputeHash(blob)) + '"';
	}
}

/// <summary>
/// A temporary image to be stored for the use of converting into ascii art.
/// </summary>
public struct TempImage : IAssetFile
{

	public byte[] Blob { get; set; }
	public string Hash { get; set; }

	/// <summary>
	/// The mime type of the image.
	/// </summary>
	public string MimeType
	{ get; set; }

	/// <param name="blob"><inheritdoc cref="TempImage.Blob" path="/summary"/></param>
	/// <param name="mimeType"><inheritdoc cref="TempImage.MimeType" path="/summary"/></param>
	public TempImage(byte[] blob = null, string mimeType = "text/plain")
	{
		Blob = blob;
		MimeType = mimeType;

		SHA1 sha1 = SHA1.Create();
		Hash = '"' + Convert.ToHexString(sha1.ComputeHash(blob)) + '"';
	}
}

public sealed class AsciiServer : HTTPServer
{
	public Cache<string, AssetFile> AssetCache = new();
	public static TimeSpan AssetTimeSpan = new(0, 12, 0);

	private AssetFile GetAssetFromCache(string path)
	{
		AssetFile cache = AssetCache[path];
		if (cache.Blob != null)
		{
			return cache;
		}

		byte[] b = File.ReadAllBytes(path),
		compressed = Helper.Compress(b);

		AssetFile asset = new(compressed);

		AssetCache[path, AssetTimeSpan] = asset;

		return asset;
	}

	public const string DefaultCacheControl = "public, max-age=3600";

	public bool HandleModified(HttpListenerContext context, AssetFile assetFile)
	{
		string noneMatch = context.Request.Headers.Get("If-None-Match");
		if (noneMatch == assetFile.Hash)
		{
			context.Response.StatusCode = (int)HttpStatusCode.NotModified;
			return true;
		}

		return false;
	}

	/// <summary>
	/// HTTP: The root HTML of the website.
	/// </summary>
	/// <param name="context">HTTP context</param>
	[UrlHandler("/", ["GET"])]
	public void Root(HttpListenerContext context)
	{
		const string HTMLPath = @"dist\index.html";
		var request = context.Request;
		var response = context.Response;

		AssetFile assetFile = GetAssetFromCache(HTMLPath);
		if (HandleModified(context, assetFile))
		{
			return;
		}

		response.AddHeader("Content-Encoding", "gzip");
		response.AddHeader("ETag", assetFile.Hash);
		response.AddHeader("Cache-Control", DefaultCacheControl);
		response.ContentType = $"text/html; charset=utf-8";
		response.ContentLength64 = assetFile.Blob.Length;
		response.StatusCode = (int)HttpStatusCode.OK;
		response.OutputStream.Write(assetFile.Blob, 0, assetFile.Blob.Length);
	}

	/// <summary>
	/// HTTP: Handles all context of the folder <c>dist/assets</c>.
	/// </summary>
	/// <param name="context">HTTP context</param>
	[UrlHandler("/assets/~", ["GET"])]
	public void Assets(HttpListenerContext context)
	{
		var response = context.Response;
		var request = context.Request;

		string assetName = context.Request.Url.Segments[2],
		assetPath = Path.Combine(@"dist\assets", assetName),
		extension = Path.GetExtension(assetName);

		const string BinaryMime = "application/octet-stream";

		try
		{
			AssetFile assetFile = GetAssetFromCache(assetPath);
			if (HandleModified(context, assetFile))
			{
				return;
			}

			response.AddHeader("Content-Encoding", "gzip");
			response.AddHeader("ETag", assetFile.Hash);
			response.AddHeader("Cache-Control", DefaultCacheControl);

			string mimetype = MimeTypeMap.GetMimeType(extension) ?? BinaryMime;
			response.ContentType = $"{mimetype}; charset=utf-8";
			response.ContentLength64 = assetFile.Blob.Length;
			response.StatusCode = (int)HttpStatusCode.OK;
			response.OutputStream.Write(assetFile.Blob, 0, assetFile.Blob.Length);
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

	// Can store 256 Megabytes
	const int MaxImageBufferSize = 1024 * 1024 * 256;

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
		stream.Read(b, 0, (int)request.ContentLength64);

		string guidString = guid.ToString();

		TempImage tempImage = new(b, mimeType);

		ImageCache[guid, TempImageTimeSpan] = tempImage;

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
			response.StatusCode = (int)code;
			return;
		}

		var tempImage = ImageCache[guid];
		if (tempImage.Blob == null)
		{
			response.StatusCode = (int)HttpStatusCode.NotFound;
			return;
		}

		byte[] b = Helper.Compress(tempImage.Blob);

		response.ContentType = tempImage.MimeType;
		response.ContentLength64 = b.Length;
		response.StatusCode = (int)HttpStatusCode.OK;
		response.AddHeader("Content-Encoding", "gzip");
		response.OutputStream.Write(b, 0, b.Length);
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
			response.StatusCode = (int)code;
			return;
		}

		var tempImage = ImageCache[guid];
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

	public AsciiServer(int port) : base(port) { }
}

#pragma warning restore CA1822 // Mark members as static