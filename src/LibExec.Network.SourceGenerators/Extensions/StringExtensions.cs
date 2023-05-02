namespace LibExec.Network.SourceGenerators.Extensions;

internal static class StringExtensions
{
    internal static string ToPropertyName(this string str)
    {
        if (str.StartsWith("_"))
        {
            str = str.Substring(1);
        }

        var firstCharacter = str.Substring(0, 1).ToUpper();
        str = str.Length > 1
            ? firstCharacter + str.Substring(1)
            : firstCharacter;

        return str;
    }
}