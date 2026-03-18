using Tic.Client.CommandLine.Commands;
using Tic.Manager;

namespace Tic.Client.CommandLine.Tests;

public class InteractiveViewLogicTests
{
    // ── Navigation ────────────────────────────────────────────────────────────

    [Fact]
    public void NavigateUp_from_first_row_stays_at_first_row()
    {
        Assert.Equal(0, InteractiveViewLogic.NavigateUp(0));
    }

    [Fact]
    public void NavigateUp_from_middle_row_moves_up_one()
    {
        Assert.Equal(1, InteractiveViewLogic.NavigateUp(2));
    }

    [Fact]
    public void NavigateDown_from_last_row_stays_at_last_row()
    {
        Assert.Equal(2, InteractiveViewLogic.NavigateDown(2, 3));
    }

    [Fact]
    public void NavigateDown_from_first_row_moves_down_one()
    {
        Assert.Equal(1, InteractiveViewLogic.NavigateDown(0, 3));
    }

    [Fact]
    public void NavigateDown_with_single_item_stays_at_zero()
    {
        Assert.Equal(0, InteractiveViewLogic.NavigateDown(0, 1));
    }

    // ── Log ordering ──────────────────────────────────────────────────────────

    [Fact]
    public void OrderLogs_sorts_by_date_then_time_ascending()
    {
        var logs = new[]
        {
            MakeLog(3, date: new DateOnly(2026, 1, 2), time: new TimeOnly(9, 0)),
            MakeLog(2, date: new DateOnly(2026, 1, 1), time: new TimeOnly(10, 0)),
            MakeLog(1, date: new DateOnly(2026, 1, 1), time: new TimeOnly(8, 0)),
        };

        var ordered = InteractiveViewLogic.OrderLogs(logs);

        Assert.Equal(1, ordered[0].Id);
        Assert.Equal(2, ordered[1].Id);
        Assert.Equal(3, ordered[2].Id);
    }

    [Fact]
    public void OrderLogs_same_date_orders_by_time()
    {
        var logs = new[]
        {
            MakeLog(2, time: new TimeOnly(12, 0)),
            MakeLog(1, time: new TimeOnly(8, 0)),
            MakeLog(3, time: new TimeOnly(17, 0)),
        };

        var ordered = InteractiveViewLogic.OrderLogs(logs);

        Assert.Equal(1, ordered[0].Id);
        Assert.Equal(2, ordered[1].Id);
        Assert.Equal(3, ordered[2].Id);
    }

    // ── Display value ─────────────────────────────────────────────────────────

    [Fact]
    public void DisplayValue_returns_empty_string_for_null()
    {
        Assert.Equal(string.Empty, InteractiveViewLogic.DisplayValue(null));
    }
    
    [Fact]
    public void DisplayValue_returns_empty_string_for_whitespace()
    {
        Assert.Equal(string.Empty, InteractiveViewLogic.DisplayValue("   "));
    }

    [Fact]
    public void DisplayValue_returns_value_when_present()
    {
        Assert.Equal("Dev", InteractiveViewLogic.DisplayValue("Dev"));
    }

    // ── Duration formatting ───────────────────────────────────────────────────

    [Fact]
    public void FormatDuration_formats_zero_duration()
    {
        Assert.Equal("00:00", InteractiveViewLogic.DisplayValue(TimeSpan.Zero));
    }

    [Fact]
    public void FormatDuration_formats_hours_minutes()
    {
        //note: seconds are truncated, not rounded
        Assert.Equal("01:30", InteractiveViewLogic.DisplayValue(new TimeSpan(1, 30, 45)));
    }

    [Fact]
    public void FormatDuration_handles_more_than_23_hours()
    {
        Assert.Equal("25:00", InteractiveViewLogic.DisplayValue(TimeSpan.FromHours(25)));
    }

    // ── Column widths ─────────────────────────────────────────────────────────

    [Fact]
    public void ComputeColumnWidths_uses_header_as_minimum_category_width()
    {
        var logs = new[] { MakeLog(1, category: "X") };
        var widths = InteractiveViewLogic.ComputeColumnWidths(logs);
        Assert.Equal("Category".Length, widths.CategoryWidth);
    }

    [Fact]
    public void ComputeColumnWidths_expands_to_long_category_value()
    {
        var logs = new[] { MakeLog(1, category: "A Very Long Category Name") };
        var widths = InteractiveViewLogic.ComputeColumnWidths(logs);
        Assert.Equal("A Very Long Category Name".Length, widths.CategoryWidth);
    }

    [Fact]
    public void ComputeColumnWidths_uses_header_as_minimum_task_width()
    {
        var logs = new[] { MakeLog(1, task: "X") };
        var widths = InteractiveViewLogic.ComputeColumnWidths(logs);
        Assert.Equal("Task".Length, widths.TaskWidth);
    }

    [Fact]
    public void ComputeColumnWidths_duration_width_matches_header()
    {
        var logs = new[] { MakeLog(1) };
        var widths = InteractiveViewLogic.ComputeColumnWidths(logs);
        Assert.Equal("Duration".Length, widths.DurationWidth);
    }

    // ── Row building ──────────────────────────────────────────────────────────

    [Fact]
    public void BuildLogRow_marks_selected_row_with_asterisk()
    {
        var widths = MakeWidths();
        var row = InteractiveViewLogic.BuildLogRow(widths, MakeLog(1), isSelected: true);
        Assert.StartsWith(">", row);
    }

    [Fact]
    public void BuildLogRow_leaves_unselected_row_with_space()
    {
        var widths = MakeWidths();
        var row = InteractiveViewLogic.BuildLogRow(widths, MakeLog(1), isSelected: false);
        Assert.StartsWith(" ", row);
    }

    [Fact]
    public void BuildLogRow_contains_date_and_time()
    {
        var widths = MakeWidths();
        var log = MakeLog(1, date: new DateOnly(2026, 3, 21), time: new TimeOnly(9, 30, 0));
        var row = InteractiveViewLogic.BuildLogRow(widths, log, isSelected: false);
        Assert.Contains("2026-03-21", row);
        Assert.Contains("09:30", row);
    }

    [Fact]
    public void BuildLogRow_uses_empty_string_for_empty_category()
    {
        var widths = MakeWidths();
        var log = MakeLog(1, category: string.Empty);
        var row = InteractiveViewLogic.BuildLogRow(widths, log, isSelected: false);
        Assert.Contains(string.Empty, row);
    }

    [Fact]
    public void BuildLogRow_includes_formatted_duration()
    {
        var widths = MakeWidths();
        var log = MakeLog(1, duration: new TimeSpan(2, 15, 0));
        var row = InteractiveViewLogic.BuildLogRow(widths, log, isSelected: false);
        Assert.Contains("02:15", row);
    }

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

    private static ColumnWidths MakeWidths() =>
        new()
        {
            CategoryWidth = 8,
            TaskWidth = 4,
            DurationWidth = 8
        };
}

