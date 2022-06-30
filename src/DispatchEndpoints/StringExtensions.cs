using System;
using System.Text;

namespace DispatchEndpoints;

internal static class StringExtensions
{
    public static string ToKebabCase(this string text)
    {
        if (text is null)
        {
            throw new ArgumentNullException(nameof(text));
        }
        if (text.Length < 2)
        {
            return text;
        }

        var sb = new StringBuilder();

        sb.Append(char.ToLowerInvariant(text[0]));

        for (var i = 1; i < text.Length; ++i)
        {
            var c = text[i];
            if (char.IsUpper(c))
            {
                sb.Append('-');
                sb.Append(char.ToLowerInvariant(c));
            }
            else
            {
                sb.Append(c);
            }
        }

        return sb.ToString();
    }

    public static string FirstCharToUpper(this string input)
        => char.ToUpper(input[0]) + input.Substring(1);
}