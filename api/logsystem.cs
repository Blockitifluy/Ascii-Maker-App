namespace ImageToAscii.Helper;

public class LogSystem(string logPath)
{
	public string LogPath = logPath;

	public void Clear()
	{
		File.WriteAllText(LogPath, "");
	}

	public void Write(object send)
	{
		File.AppendAllText(LogPath, "\n" + send.ToString());
	}
}