using Tic.Client.CommandLine.UI;
using Tic.Manager;

namespace Tic.Client.CommandLine.Tests;

public class NavigatorTests
{
    // ── Navigation ────────────────────────────────────────────────────────────

    [Fact]
    public void NavigateUp_from_first_row_stays_at_first_row()
    {
        Assert.Equal(0, Navigator.Up(0));
    }

    [Fact]
    public void NavigateUp_from_middle_row_moves_up_one()
    {
        Assert.Equal(1, Navigator.Up(2));
    }

    [Fact]
    public void NavigateDown_from_last_row_stays_at_last_row()
    {
        Assert.Equal(2, Navigator.Down(2, 3));
    }

    [Fact]
    public void NavigateDown_from_first_row_moves_down_one()
    {
        Assert.Equal(1, Navigator.Down(0, 3));
    }

    [Fact]
    public void NavigateDown_with_single_item_stays_at_zero()
    {
        Assert.Equal(0, Navigator.Down(0, 1));
    }
    
    // ── Display value ─────────────────────────────────────────────────────────

    [Fact]
    public void DisplayValue_returns_empty_string_for_null()
    {
        Assert.Equal(string.Empty, Converter.DisplayValue(null));
    }
    
    [Fact]
    public void DisplayValue_returns_empty_string_for_whitespace()
    {
        Assert.Equal(string.Empty, Converter.DisplayValue("   "));
    }

    [Fact]
    public void DisplayValue_returns_value_when_present()
    {
        Assert.Equal("Dev", Converter.DisplayValue("Dev"));
    }

    // ── Duration formatting ───────────────────────────────────────────────────

    [Fact]
    public void FormatDuration_formats_zero_duration()
    {
        Assert.Equal("00:00", Converter.DisplayValue(TimeSpan.Zero));
    }

    [Fact]
    public void FormatDuration_formats_hours_minutes()
    {
        //note: seconds are truncated, not rounded
        Assert.Equal("01:30", Converter.DisplayValue(new TimeSpan(1, 30, 45)));
    }

    [Fact]
    public void FormatDuration_handles_more_than_23_hours()
    {
        Assert.Equal("25:00", Converter.DisplayValue(TimeSpan.FromHours(25)));
    }
    
    // // ── Row building ──────────────────────────────────────────────────────────
    //
    // [Fact]
    // public void BuildLogRow_marks_selected_row_with_arrow()
    // {
    //     var widths = MakeWidths();
    //     var row = Navigator.BuildLogRow(widths, MakeLog(1), isSelected: true);
    //     Assert.StartsWith(">", row);
    // }
    //
    // [Fact]
    // public void BuildLogRow_leaves_unselected_row_with_space()
    // {
    //     var widths = MakeWidths();
    //     var row = Navigator.BuildLogRow(widths, MakeLog(1), isSelected: false);
    //     Assert.StartsWith(" ", row);
    // }
    
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static TimeLogsResponseItem MakeLog(
        int id,
        DateOnly? date = null,
        TimeOnly? time = null,
        string category = "Dev",
        string task = "CLI",
        string description = "Test entry",
        TimeSpan? duration = null) =>
        new()
        {
            Id = id,
            Date = date ?? new DateOnly(2026, 1, 1),
            Time = time ?? new TimeOnly(9, 0, 0),
            Category = category,
            Task = task,
            Description = description,
            Duration = duration ?? TimeSpan.Zero
        };

    private static Dictionary<string, int> MakeWidths() =>
        new()
        {
            ["Category"] = 8,
            ["Project"] = 8,
            ["Task"] = 4,
            ["Duration"] = 8
        };
}

