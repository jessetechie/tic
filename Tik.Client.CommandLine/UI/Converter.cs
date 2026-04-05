namespace Tik.Client.CommandLine.UI;

internal static class Converter
{
    public static bool TryParseDate(string? value, out DateOnly date, out string error)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            date = DateOnly.FromDateTime(DateTime.Today);
            error = string.Empty;
            return true;
        }

        if (DateOnly.TryParse(value, out date))
        {
            error = string.Empty;
            return true;
        }

        error = "Invalid --date value. Use a valid date like 2026-03-21.";
        return false;
    }

    public static bool TryParseTime(string? value, out TimeOnly time, out string error)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            time = TimeOnly.FromDateTime(DateTime.Now);
            error = string.Empty;
            return true;
        }

        if (TimeOnly.TryParse(value, out time))
        {
            error = string.Empty;
            return true;
        }

        error = "Invalid --time value. Use a valid time like 09:30 or 09:30:00.";
        return false;
    }
    
    internal static string DisplayValue(DateOnly date) =>
        date.ToString("yyyy-MM-dd");
    
    internal static string DisplayValue(TimeOnly time) =>
        time.ToString("HH:mm");
    
    internal static string DisplayValue(TimeSpan duration)
    {
        var totalHours = (int)duration.TotalHours;
        return $"{totalHours:00}:{duration.Minutes:00}";
    }

    internal static string DisplayValue(int value) =>
        value.ToString();
    
    internal static string DisplayValue(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "" : value;

}