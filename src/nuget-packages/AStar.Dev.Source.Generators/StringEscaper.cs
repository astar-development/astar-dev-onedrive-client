namespace AStar.Dev.Source.Generators;

internal static class StringEscaper
{
    public static string ToCSharpString(string s) => "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";

    public static string Escape(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"");
}
