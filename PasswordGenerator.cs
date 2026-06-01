using System.Security.Cryptography;

namespace EntropyPasswordForge_CS;

internal sealed record PasswordOptions(
    int Length,
    int Count,
    bool UseLower,
    bool UseUpper,
    bool UseDigits,
    bool UseSymbols,
    string SymbolCharacters,
    bool RequireEveryCustomSymbol,
    bool ExcludeAmbiguous,
    bool RequireEachSelectedType,
    int MinimumUpper,
    int MinimumDigits,
    int MinimumSymbols);

internal static class PasswordGenerator
{
    private const string Lower = "abcdefghijklmnopqrstuvwxyz";
    private const string Upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private const string Digits = "0123456789";
    private const string Ambiguous = "0Oo1lI|`'\"";

    public static List<string> Generate(PasswordOptions options, byte[] seed)
    {
        ValidateOptions(options);
        using HmacSha512Drbg drbg = new(seed);

        CharacterSets sets = BuildCharacterSets(options);
        List<string> requiredGroups = BuildRequiredGroups(options, sets);
        string charset = sets.AllCharacters;
        List<string> passwords = new(options.Count);

        for (int i = 0; i < options.Count; i++)
        {
            passwords.Add(GenerateOne(options.Length, requiredGroups, charset, drbg));
        }

        return passwords;
    }

    private static string GenerateOne(int length, List<string> requiredGroups, string charset, HmacSha512Drbg drbg)
    {
        char[] chars = new char[length];
        int index = 0;

        if (requiredGroups.Count > length)
        {
            throw new InvalidOperationException("指定された配分の合計がパスワード桁数を超えています。桁数を増やすか、配分スライダーを下げてください。");
        }

        foreach (string group in requiredGroups)
        {
            chars[index++] = group[drbg.NextInt32(group.Length)];
        }

        while (index < chars.Length)
        {
            chars[index++] = charset[drbg.NextInt32(charset.Length)];
        }

        Shuffle(chars, drbg);
        return new string(chars);
    }

    private static void Shuffle(char[] chars, HmacSha512Drbg drbg)
    {
        for (int i = chars.Length - 1; i > 0; i--)
        {
            int j = drbg.NextInt32(i + 1);
            (chars[i], chars[j]) = (chars[j], chars[i]);
        }
    }

    private static CharacterSets BuildCharacterSets(PasswordOptions options)
    {
        string lower = options.UseLower ? FilterAmbiguous(Lower, options.ExcludeAmbiguous) : string.Empty;
        string upper = options.UseUpper ? FilterAmbiguous(Upper, options.ExcludeAmbiguous) : string.Empty;
        string digits = options.UseDigits ? FilterAmbiguous(Digits, options.ExcludeAmbiguous) : string.Empty;
        string symbols = options.UseSymbols ? FilterAmbiguous(options.SymbolCharacters, options.ExcludeAmbiguous) : string.Empty;

        ValidateEnabledSet(lower, options.UseLower, "英小文字");
        ValidateEnabledSet(upper, options.UseUpper, "英大文字");
        ValidateEnabledSet(digits, options.UseDigits, "数字");
        ValidateEnabledSet(symbols, options.UseSymbols, "記号");

        string all = string.Concat(lower, upper, digits, symbols);
        if (all.Length == 0)
        {
            throw new InvalidOperationException("少なくとも1つの文字種を選択してください。");
        }

        return new CharacterSets(lower, upper, digits, symbols, all);
    }

    private static List<string> BuildRequiredGroups(PasswordOptions options, CharacterSets sets)
    {
        List<string> groups = [];

        if (options.RequireEachSelectedType && options.UseLower)
        {
            groups.Add(sets.Lower);
        }

        AddRepeatedGroups(groups, sets.Upper, options.UseUpper, Math.Max(options.MinimumUpper, options.RequireEachSelectedType ? 1 : 0));
        AddRepeatedGroups(groups, sets.Digits, options.UseDigits, Math.Max(options.MinimumDigits, options.RequireEachSelectedType ? 1 : 0));

        if (options.UseSymbols)
        {
            int symbolMinimum = Math.Max(options.MinimumSymbols, options.RequireEachSelectedType ? 1 : 0);

            if (options.RequireEveryCustomSymbol)
            {
                foreach (char symbol in sets.Symbols)
                {
                    groups.Add(symbol.ToString());
                }

                AddRepeatedGroups(groups, sets.Symbols, true, Math.Max(0, symbolMinimum - sets.Symbols.Length));
            }
            else
            {
                AddRepeatedGroups(groups, sets.Symbols, true, symbolMinimum);
            }
        }

        if (groups.Count > options.Length)
        {
            throw new InvalidOperationException("指定された配分の合計がパスワード桁数を超えています。桁数を増やすか、配分スライダーを下げてください。");
        }

        return groups;
    }

    private static void AddRepeatedGroups(List<string> groups, string source, bool enabled, int count)
    {
        if (!enabled || count <= 0)
        {
            return;
        }

        if (source.Length == 0)
        {
            throw new InvalidOperationException("選択された文字種の文字集合が空です。設定を見直してください。");
        }

        for (int i = 0; i < count; i++)
        {
            groups.Add(source);
        }
    }

    private static void ValidateEnabledSet(string source, bool enabled, string name)
    {
        if (enabled && source.Length == 0)
        {
            throw new InvalidOperationException($"{name}の文字集合が空です。設定を見直してください。");
        }
    }

    private static string FilterAmbiguous(string source, bool excludeAmbiguous)
    {
        return excludeAmbiguous
            ? new string(source.Where(c => !Ambiguous.Contains(c, StringComparison.Ordinal)).ToArray())
            : source;
    }

    private static void ValidateOptions(PasswordOptions options)
    {
        if (options.Length is < 16 or > 256)
        {
            throw new ArgumentOutOfRangeException(nameof(options.Length));
        }

        if (options.Count is < 1 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(options.Count));
        }

        if (options.MinimumUpper is < 0 or > 20 || options.MinimumDigits is < 0 or > 20 || options.MinimumSymbols is < 0 or > 20)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "配分スライダー値が範囲外です。");
        }
    }

    private sealed record CharacterSets(string Lower, string Upper, string Digits, string Symbols, string AllCharacters);
}
