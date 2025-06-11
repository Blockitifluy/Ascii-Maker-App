namespace ImageToAscii.HelperClasses;

public class LogSystem(string logPath)
{
	public string LogPath = logPath;

	public void Clear()
	{
		File.WriteAllText(LogPath, "");
	}

	public void Write(object send)
	{
		DateTime time = DateTime.Now;
		string timeString = time.ToString();

		string msg = $"[{timeString}] {send.ToString()}\n";

		File.AppendAllText(LogPath, msg);
	}
}