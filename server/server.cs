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

public sealed partial class AsciiMakerServer
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
			Loop();
		}

		Listener.Close();
		Console.WriteLine("\nEnded http server!");
	}

	private bool TryGetRequestToHandler(HttpListenerRequest request, out HandlerData outHandler)
	{
		foreach (HandlerData handler in UrlHandlers.Values)
		{
			string handlerURL = handler.Path,
			url = request.Url.LocalPath;
			int handleLength = handlerURL.Length;

			// Cut the query parameters
			int index = url.IndexOf('?');
			if (index >= 0)
				url = url[..index];

			if (handlerURL == url && handler.Method.Contains(request.HttpMethod))
			{
				outHandler = handler;
				return true;
			}

			// Remove extention marker
			if (!handlerURL.EndsWith('~') || handleLength - 2 > url.Length) continue;

			int cutLength = handleLength - 2;

			string matchModURL = handlerURL[..cutLength];
			string modURL = url[..cutLength];

			if (matchModURL == modURL)
			{
				outHandler = handler;
				return true;
			}
		}

		outHandler = new();
		return false;
	}

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
			byte[] notFound = Encoding.UTF8.GetBytes("404 PAGE NOT FOUND");

			response.StatusCode = (int)HttpStatusCode.NotFound;
			response.ContentType = "text/plain";
			response.OutputStream.Write(notFound, 0, notFound.Length);
			return;
		}

		handler.UrlHandler(context);
	}

	// 24 Megabytes
	public const long MaxRequestBodyLength = 1024 * 1024 * 24;

	// 512 Kilobytes
	public const long MaxRequestURLLength = 512;

	private void Loop()
	{
		HttpListenerContext context = Listener.GetContext();
		HttpListenerResponse response = context.Response;
		HttpListenerRequest request = context.Request;

		string localPath = request.Url.LocalPath;

		if (request.ContentLength64 > MaxRequestBodyLength)
		{
			response.StatusCode = (int)HttpStatusCode.RequestEntityTooLarge;
			response.Close();
			return;
		}
		else if (localPath.Length > MaxRequestBodyLength)
		{
			response.StatusCode = (int)HttpStatusCode.RequestUriTooLong;
			response.Close();
			return;
		}

		Console.WriteLine($"> Recieved request ({localPath})");

		try
		{
			HandleURLRequest(context);
		}
		catch (Exception ex)
		{
			Console.WriteLine(ex);
			response.Abort();
			return;
		}

		Console.WriteLine($"\tStatus Code: {response.StatusCode}.");
		Console.WriteLine($"\tContent Length: {response.ContentLength64}.");

		response.Close();
	}

	public AsciiMakerServer(int port)
	{
		Port = port;

		Listener.Prefixes.Add($"http://127.0.0.1:{port}/");
		Listener.Prefixes.Add($"http://localhost:{port}/");

		LoadUrlHandlers();
	}
}