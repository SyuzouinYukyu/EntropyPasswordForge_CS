namespace EntropyPasswordForge_CS;

internal sealed class AppSettings
{
    public int PasswordLength { get; set; } = 16;
    public int PasswordCount { get; set; } = 1;
    public bool UseLower { get; set; } = true;
    public bool UseUpper { get; set; } = true;
    public bool UseDigits { get; set; } = true;
    public bool UseSymbols { get; set; } = true;
    public bool ExcludeAmbiguous { get; set; }
    public bool RequireEachSelectedType { get; set; } = true;
    public bool ClearClipboardAfterCopy { get; set; } = true;
    public bool AutoCopy { get; set; }
    public bool AutoClearResults { get; set; } = true;
    public int MinimumUpper { get; set; } = 4;
    public int MinimumDigits { get; set; } = 6;
    public int MinimumSymbols { get; set; } = 5;
    public int SymbolModeIndex { get; set; }
    public int CustomSymbolUsageIndex { get; set; }
    public string CustomSymbols { get; set; } = string.Empty;
    public float ResultFontSize { get; set; } = 10f;
    public int MainSplitterDistance { get; set; } = 378;
    public int LogSplitterDistance { get; set; } = 590;
    public int WindowWidth { get; set; } = 1280;
    public int WindowHeight { get; set; } = 760;
    public int WindowLeft { get; set; } = int.MinValue;
    public int WindowTop { get; set; } = int.MinValue;
    public int SelectedTabIndex { get; set; }
}
