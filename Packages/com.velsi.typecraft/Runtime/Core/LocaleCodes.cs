namespace Velsi.Typecraft
{
    public static class LocaleCodes
    {
        public static string Language(string locale)
        {
            if (string.IsNullOrEmpty(locale))
                return string.Empty;

            int separator = locale.IndexOfAny(new[] { '-', '_' });
            return separator > 0 ? locale.Substring(0, separator) : locale;
        }
    }
}
