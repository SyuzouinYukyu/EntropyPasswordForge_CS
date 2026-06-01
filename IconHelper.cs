namespace EntropyPasswordForge_CS;

internal static class IconHelper
{
    public static Icon? LoadApplicationIcon()
    {
        try
        {
            Icon? icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            if (icon is not null)
            {
                return icon;
            }
        }
        catch
        {
        }

        try
        {
            string localIcon = Path.Combine(AppContext.BaseDirectory, "icon.ico");
            return File.Exists(localIcon) ? new Icon(localIcon) : null;
        }
        catch
        {
            return null;
        }
    }
}
