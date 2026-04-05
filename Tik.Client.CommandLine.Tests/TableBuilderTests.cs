using Tik.Client.CommandLine.UI;

namespace Tik.Client.CommandLine.Tests;

public class TableBuilderTests
{
    [Fact]
    public void CalculateWidthsFromProperties_uses_header_as_minimum_width()
    {
        var headers = new Dictionary<string, string> { ["Name"] = "Name" };
        var columnOrder = new[] { "Name" };
        var data = new[] { new { Name = "A" } };
        
        var calculator = new TableBuilder(headers, columnOrder);
        calculator.CalculateWidthsFromProperties(data);
        
        Assert.Equal(4, calculator.GetColumnWidth("Name")); // "Name".Length = 4
    }

    [Fact]
    public void CalculateWidthsFromProperties_expands_to_data_width()
    {
        var headers = new Dictionary<string, string> { ["Name"] = "Name" };
        var columnOrder = new[] { "Name" };
        var data = new[] { new { Name = "A Very Long Name" } };
        
        var calculator = new TableBuilder(headers, columnOrder);
        calculator.CalculateWidthsFromProperties(data);
        
        Assert.Equal(16, calculator.GetColumnWidth("Name")); // "A Very Long Name".Length = 16
    }

    [Fact]
    public void CalculateWidthsFromData_with_selectors_works_correctly()
    {
        var headers = new Dictionary<string, string> { ["Name"] = "Name" };
        var columnOrder = new[] { "Name" };
        var data = new[] { new { FullName = "John Doe" } };
        var selectors = new Dictionary<string, Func<dynamic, string>> 
        { 
            ["Name"] = x => x.FullName 
        };
        
        var calculator = new TableBuilder(headers, columnOrder);
        calculator.CalculateWidthsFromData(data, selectors);
        
        Assert.Equal(8, calculator.GetColumnWidth("Name")); // "John Doe".Length = 8
    }

    [Fact]
    public void CreateHeaderRow_formats_correctly_with_alignments()
    {
        var headers = new Dictionary<string, string> 
        { 
            ["Id"] = "Id", 
            ["Name"] = "Name" 
        };
        var columnOrder = new[] { "Id", "Name" };
        var alignments = new Dictionary<string, ColumnAlignment> 
        { 
            ["Id"] = ColumnAlignment.Right 
        };
        var data = new[] { new { Id = "12345", Name = "John Smith" } };
        
        var calculator = new TableBuilder(headers, columnOrder, "  ", alignments);
        calculator.CalculateWidthsFromProperties(data);
        
        var header = calculator.CreateHeaderRow();
        
        Assert.Equal("   Id  Name      ", header);
    }

    [Fact]
    public void CreateSeparatorRow_creates_dashes_matching_widths()
    {
        var headers = new Dictionary<string, string> { ["Id"] = "Id", ["Name"] = "Name" };
        var columnOrder = new[] { "Id", "Name" };
        var data = new[] { new { Id = "1", Name = "12345" } };
        
        var calculator = new TableBuilder(headers, columnOrder);
        calculator.CalculateWidthsFromProperties(data);
        
        var separator = calculator.CreateSeparatorRow();
        
        Assert.Equal("--  -----", separator);
    }

    [Fact]
    public void CreateDataRow_formats_values_with_alignments()
    {
        var headers = new Dictionary<string, string> { ["Id"] = "Id", ["Name"] = "Name" };
        var columnOrder = new[] { "Id", "Name" };
        var alignments = new Dictionary<string, ColumnAlignment> { ["Id"] = ColumnAlignment.Right };
        var data = new[] { new { Id = "12345", Name = "John Smith" } };
        
        var calculator = new TableBuilder(headers, columnOrder, "  ", alignments);
        calculator.CalculateWidthsFromProperties(data);
        
        var values = new Dictionary<string, string> { ["Id"] = "1", ["Name"] = "John" };
        var dataRow = calculator.CreateDataRow(values);
        
        Assert.Equal("    1  John      ", dataRow);
    }
}
