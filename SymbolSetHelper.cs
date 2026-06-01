namespace EntropyPasswordForge_CS;

internal enum SymbolSetMode
{
    Standard,
    Custom
}

internal enum CustomSymbolUsageMode
{
    Include,
    Exclude
}

internal static class SymbolSetHelper
{
    public const string StandardSymbols = "!#$%&()*+,-./:;<=>?@[]^_{~}";
    public const string DefaultCustomSymbols = "!#$%&()*+,-./:;<=>?@[]^_{|}~";

    public static string GetSymbols(SymbolSetMode mode, string customSymbols)
    {
        string source = mode switch
        {
            SymbolSetMode.Standard => StandardSymbols,
            SymbolSetMode.Custom => customSymbols,
            _ => StandardSymbols
        };

        return NormalizeSymbols(source);
    }

    public static string GetDisplayName(SymbolSetMode mode)
    {
        return mode switch
        {
            SymbolSetMode.Standard => "標準記号",
            SymbolSetMode.Custom => "カスタム記号",
            _ => "標準記号"
        };
    }

    public static string GetCustomUsageDisplayName(CustomSymbolUsageMode mode)
    {
        return mode == CustomSymbolUsageMode.Exclude ? "除外" : "含む";
    }

    public static string ExcludeFromStandard(string excludedSymbols)
    {
        string excluded = NormalizeSymbols(excludedSymbols);
        return new string(StandardSymbols.Where(c => !excluded.Contains(c, StringComparison.Ordinal)).ToArray());
    }

    public static string NormalizeSymbols(string source)
    {
        HashSet<char> seen = [];
        List<char> chars = [];
        foreach (char c in source)
        {
            if (char.IsWhiteSpace(c) || seen.Contains(c))
            {
                continue;
            }

            seen.Add(c);
            chars.Add(c);
        }

        return new string(chars.ToArray());
    }
}
