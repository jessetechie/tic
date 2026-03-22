namespace Tic.Client.CommandLine.UI;

/// <summary>
/// Calculates column widths for tabular data display.
/// </summary>
public class TableBuilder
{
    private readonly Dictionary<string, string> _headers;
    private readonly string[] _columnOrder;
    private readonly Dictionary<string, ColumnAlignment> _alignments;
    private readonly string _separator;
    private readonly Dictionary<string, int> _widths;

    /// <summary>
    /// Initializes a new instance of TableColumnWidthCalculator.
    /// </summary>
    /// <param name="headers">Dictionary of column name to header display text</param>
    /// <param name="columnOrder">Order of columns to display</param>
    /// <param name="separator">Column separator (default: "  ")</param>
    /// <param name="alignments">Optional column alignments (Left by default)</param>
    public TableBuilder(
        Dictionary<string, string> headers,
        string[] columnOrder,
        string separator = "  ",
        Dictionary<string, ColumnAlignment>? alignments = null)
    {
        _headers = headers;
        _columnOrder = columnOrder;
        _separator = separator;
        _alignments = alignments ?? new Dictionary<string, ColumnAlignment>();
        _widths = new Dictionary<string, int>();
        
        // Initialize widths to header lengths as minimum
        foreach (var (columnName, headerText) in _headers)
        {
            _widths[columnName] = headerText.Length;
        }
    }

    /// <summary>
    /// Calculates column widths based on data content using selector functions.
    /// </summary>
    /// <param name="data">Collection of data rows</param>
    /// <param name="columnValueSelectors">Dictionary of column name to value selector functions</param>
    public void CalculateWidthsFromData<T>(IEnumerable<T> data, Dictionary<string, Func<T, string>> columnValueSelectors)
    {
        var dataArray = data.ToArray();
        
        foreach (var (columnName, selector) in columnValueSelectors)
        {
            if (!_headers.TryGetValue(columnName, out var header)) continue;
            
            var headerWidth = header.Length;
            var maxDataWidth = 0;

            if (dataArray.Length > 0)
            {
                maxDataWidth = dataArray.Select(selector).Select(value => value.Length).DefaultIfEmpty(0).Max();
            }

            _widths[columnName] = Math.Max(headerWidth, maxDataWidth);
        }
    }

    /// <summary>
    /// Calculates column widths using reflection to extract property values.
    /// Properties must be of type string or have ToString() called on them.
    /// </summary>
    /// <param name="data">Collection of data objects</param>
    public void CalculateWidthsFromProperties<T>(IEnumerable<T> data)
    {
        var dataArray = data.ToArray();
        var typeInfo = typeof(T);

        foreach (var (propertyName, headerText) in _headers)
        {
            var headerWidth = headerText.Length;
            var maxDataWidth = 0;

            var property = typeInfo.GetProperty(propertyName);
            if (property != null && dataArray.Length > 0)
            {
                var values = dataArray
                    .Select(item => property.GetValue(item)?.ToString() ?? string.Empty)
                    .Select(value => value.Length);
                
                maxDataWidth = values.DefaultIfEmpty(0).Max();
            }

            _widths[propertyName] = Math.Max(headerWidth, maxDataWidth);
        }
    }

    /// <summary>
    /// Creates a header row string with proper column padding.
    /// </summary>
    /// <returns>Formatted header row</returns>
    public string CreateHeaderRow()
    {
        var columns = _columnOrder
            .Where(col => _headers.ContainsKey(col) && _widths.ContainsKey(col))
            .Select(col =>
            {
                var header = _headers[col];
                var width = _widths[col];
                var alignment = _alignments.GetValueOrDefault(col, ColumnAlignment.Left);
                
                return alignment switch
                {
                    ColumnAlignment.Right => header.PadLeft(width),
                    ColumnAlignment.Center => header.PadLeft((width + header.Length) / 2).PadRight(width),
                    _ => header.PadRight(width)
                };
            });

        return string.Join(_separator, columns);
    }

    /// <summary>
    /// Creates a separator row with dashes matching column widths.
    /// </summary>
    /// <returns>Formatted separator row</returns>
    public string CreateSeparatorRow()
    {
        var columns = _columnOrder
            .Where(_widths.ContainsKey)
            .Select(col => new string('-', _widths[col]));

        return string.Join(_separator, columns);
    }

    /// <summary>
    /// Creates a data row string with proper column padding.
    /// </summary>
    /// <param name="values">Dictionary of column name to value</param>
    /// <returns>Formatted data row</returns>
    public string CreateDataRow(Dictionary<string, string> values)
    {
        var columns = _columnOrder
            .Where(col => values.ContainsKey(col) && _widths.ContainsKey(col))
            .Select(col =>
            {
                var value = values[col];
                var width = _widths[col];
                var alignment = _alignments.GetValueOrDefault(col, ColumnAlignment.Left);
                
                return alignment switch
                {
                    ColumnAlignment.Right => value.PadLeft(width),
                    ColumnAlignment.Center => value.PadLeft((width + value.Length) / 2).PadRight(width),
                    _ => value.PadRight(width)
                };
            });

        return string.Join(_separator, columns);
    }

    /// <summary>
    /// Gets the calculated width for a specific column.
    /// </summary>
    /// <param name="columnName">The column name</param>
    /// <returns>The calculated width, or 0 if column not found</returns>
    public int GetColumnWidth(string columnName)
    {
        return _widths.GetValueOrDefault(columnName, 0);
    }

    /// <summary>
    /// Gets all calculated column widths.
    /// </summary>
    /// <returns>Dictionary of column name to calculated width</returns>
    public Dictionary<string, int> GetAllColumnWidths()
    {
        return new Dictionary<string, int>(_widths);
    }
}

/// <summary>
/// Column alignment options for table formatting.
/// </summary>
public enum ColumnAlignment
{
    Left,
    Right,
    Center
}
