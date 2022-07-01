using System;
using System.Text;
using System.Text.RegularExpressions;

namespace DispatchEndpoints;

internal static class StringExtensions
{
    public static string PascalToKebabCase(this string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        return Regex.Replace(
            value,
            "(?<!^)([A-Z][a-z]|(?<=[a-z])[A-Z])",
            "-$1",
            RegexOptions.Compiled
        )
            .Trim()
            .ToLower();
    }

    public static string FirstCharToUpper(this string input)
        => char.ToUpper(input[0]) + input.Substring(1);
}