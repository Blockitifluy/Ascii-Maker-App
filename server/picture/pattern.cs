using System.Text.Json.Serialization;

namespace ImageToAscii.Picture;

public class Pattern
{
	public List<string> Characters { get; set; }
	public string Name { get; set; }

	public PatternSet[] GetPatternSet()
	{
		PatternSet[] sets = new PatternSet[Characters.Count];

		for (int i = 0; i < Characters.Count; i++)
		{
			float pos = (float)i / Characters.Count;
			string pat = Characters[i];

			PatternSet set = new(pat, pos);

			sets[i] = set;
		}

		return sets;
	}

	public Pattern()
	{

	}
}

public struct PatternSet(string ch, float pos)
{
	public string Char = ch;
	public float Position = pos;
}
