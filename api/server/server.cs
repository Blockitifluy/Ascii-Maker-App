using System.Diagnostics;
using System.Net;
using System.Reflection;
using System.Text;

namespace ImageToAscii.Server;

[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
public sealed class UrlHandlerAttribute(string urlPath, string[] methods) : Attribute
{
	public readonly string UrlPath = urlPath;
	public readonly string[] Methods = methods;
}

/// <summary>
/// Delegate for handling URL requests in the HTTP server.
/// </summary>
/// <param name="context">The context of the http request (eg. url, request headers)</param>
public delegate void UrlHandler(HttpListenerContext context);

/// <summary>
/// Struct to hold the URL handler data, including the local path and the handler delegate.
/// </summary>
/// <param name="path">The local path of the URL handler.</param>
/// <param name="urlHandler">The delegate that handles the URL request.</param>
public struct HandlerData(string path, string[] methods, UrlHandler urlHandler)
{
	public string Path = path;
	public string[] Method = methods;
	public UrlHandler UrlHandler = urlHandler;
}

public abstract class HTTPServer
{
	public int Port;
	public HttpListener Listener = new();

	private static bool IsClosing = false;

	public Dictionary<string, HandlerData> UrlHandlers = [];

	/// <summary>
	/// Loads URL handlers from the current assembly by searching for methods with the <see cref="UrlHandlerAttribute"/> attribute.
	/// Each method found is registered as a URL handler for the specified local path.
	/// </summary>
	private void LoadUrlHandlers()
	{
		Type selfType = GetType();
		MethodInfo[] methodInfos = selfType.GetMethods();

		try
		{
			foreach (MethodInfo method in methodInfos)
			{
				var handlerAttributes = (UrlHandlerAttribute[])method.GetCustomAttributes<UrlHandlerAttribute>();

				if (handlerAttributes.Length == 0)
					continue;

				var handler = method.CreateDelegate<UrlHandler>(this);

				foreach (var handlerAtt in handlerAttributes)
				{
					HandlerData data = new(handlerAtt.UrlPath, handlerAtt.Methods, handler);
					UrlHandlers.Add(handlerAtt.UrlPath, data);
				}
			}
		}
		catch (Exception)
		{
			throw;
		}
	}

	public void Start()
	{
		Listener.Start();

		Console.WriteLine("\nStarted http server!");

		Console.CancelKeyPress += (sender, e) =>
		{
			if (IsClosing)
			{
				Console.WriteLine("Server is already shutting down...");
				return;
			}

			e.Cancel = true;
			IsClosing = true;
			Console.WriteLine("Shutting down server on next server request...");
		};

		while (!IsClosing)
		{
			HttpListenerContext context = Listener.GetContext();

			if (context == null)
				return;

			ProcessContext(context);
		}

		Listener.Close();
		Console.WriteLine("\nEnded http server!");
	}

	const string ExtentionMaker = "/~";

	/// <summary>
	/// Trys to get the handler approprate for the request.
	/// </summary>
	/// <param name="request">HTTP Request to be handled.</param>
	/// <param name="outHandler">The handler approprate for the request.</param>
	/// <returns>Has a handler been found.</returns>
	private bool TryGetRequestToHandler(HttpListenerRequest request, out HandlerData outHandler)
	{
		string url = request.Url.LocalPath;

		foreach (HandlerData handler in UrlHandlers.Values)
		{
			// Case 1 - Extact match

			string handlerURL = handler.Path;
			int handleLength = handlerURL.Length;

			bool areURLsExact = handlerURL == url,
			methodsMatch = handler.Method.Contains(request.HttpMethod);

			if (areURLsExact && methodsMatch)
			{
				outHandler = handler;
				return true;
			}

			// Case 2 - Extention Maker

			int extentionIndex = handleLength - ExtentionMaker.Length;

			bool endsInMaker = handlerURL.EndsWith('~'),
			urlFits = extentionIndex > url.Length;
			if (!endsInMaker || urlFits) continue;

			string matchModURL = handlerURL[..extentionIndex],
			modURL = url[..extentionIndex];

			if (matchModURL == modURL)
			{
				outHandler = handler;
				return true;
			}
		}

		outHandler = new();
		return false;
	}

	public static string NotFoundMessage = "404 PAGE NOT FOUND";

	/// <summary>
	/// Handles incoming HTTP requests by matching the requested URL to a registered handler.
	/// If no handler is found for the requested URL, responds with a 404 status and a "PAGE NOT FOUND" message.
	/// </summary>
	/// <param name="context">The <see cref="HttpListenerContext"/> object that provides access to the request and response objects.</param>
	/// <param name="response">The <see cref="HttpListenerResponse"/> object used to send a response back to the client.</param>
	private void HandleURLRequest(HttpListenerContext context)
	{
		var response = context.Response;

		if (!TryGetRequestToHandler(context.Request, out HandlerData handler))
		{
			byte[] notFound = Encoding.UTF8.GetBytes(NotFoundMessage);

			response.StatusCode = (int)HttpStatusCode.NotFound;
			response.ContentType = "text/plain";
			response.OutputStream.Write(notFound, 0, notFound.Length);
			return;
		}

		handler.UrlHandler(context);
	}

	// 24 Megabytes
	public const long MaxRequestBodyLength = 1024 * 1024 * 24;

	// 512 Bytes
	public const long MaxRequestURLLength = 512;

	public static string ErrorMessage = "Error when processing request. Please try again later";

	public bool IsRequestValid(HttpListenerRequest request, out int code)
	{
		string localPath = request.Url.LocalPath;

		if (request.ContentLength64 > MaxRequestBodyLength)
		{
			code = (int)HttpStatusCode.RequestEntityTooLarge;
			return false;
		}
		else if (localPath.LongCount() > MaxRequestBodyLength)
		{
			code = (int)HttpStatusCode.RequestUriTooLong;
			return false;
		}

		code = 0;
		return true;
	}

	private void OnContextException(HttpListenerResponse response, Exception ex, string localPath)
	{
		Console.ForegroundColor = ConsoleColor.Red;
		Console.WriteLine($"\tServer has expierenced an error while handling request ({localPath}), see {Program.LogPath} more info!");
		Console.ResetColor();

		Program.LogSystem.Write(ex);

		byte[] errorMsg = Encoding.UTF8.GetBytes(ErrorMessage);

		response.ContentLength64 = errorMsg.Length;
		response.StatusCode = (int)HttpStatusCode.InternalServerError;
		response.OutputStream.Write(errorMsg, 0, errorMsg.Length);
	}

	public void ProcessContext(HttpListenerContext context)
	{
		HttpListenerResponse response = context.Response;
		HttpListenerRequest request = context.Request;

		string localPath = request.Url.LocalPath;

		if (!IsRequestValid(request, out var code))
		{
			response.StatusCode = code;
			response.Close();
			return;
		}

		Stopwatch timer = new();
		timer.Start();

		try
		{
			Console.ForegroundColor = ConsoleColor.Cyan;
			Console.WriteLine($"> Handling request ({localPath})");
			Console.ResetColor();

			HandleURLRequest(context);
		}
		catch (Exception ex)
		{
			OnContextException(response, ex, localPath);
			return;
		}
		finally
		{
			timer.Stop();
			Console.WriteLine($"\tRequest took {timer.ElapsedMilliseconds}ms");
			response.Close();
		}

		Console.WriteLine($"\tStatus Code: {response.StatusCode}.");
		Console.WriteLine($"\tContent Length: {response.ContentLength64}.");
	}

	public HTTPServer(int port)
	{
		Port = port;

		Listener.Prefixes.Add($"http://127.0.0.1:{port}/");
		Listener.Prefixes.Add($"http://localhost:{port}/");

		LoadUrlHandlers();
	}
}