using System.Data;
using System.Text.Json;
using Dapper;

namespace Tik.ResourceAccess;

internal abstract class SqliteTypeHandler<T> : SqlMapper.TypeHandler<T>
{
    // Parameters are converted by Microsoft.Data.Sqlite
    public override void SetValue(IDbDataParameter parameter, T? value)
        => parameter.Value = value;
}

internal class GuidHandler : SqliteTypeHandler<Guid>
{
    public override Guid Parse(object value)
        => Guid.Parse((string)value);
}

internal class DateTimeOffsetHandler : SqliteTypeHandler<DateTimeOffset>
{
    public override DateTimeOffset Parse(object value)
        => DateTimeOffset.Parse((string)value);
}

internal class DateOnlyHandler : SqliteTypeHandler<DateOnly>
{
    public override DateOnly Parse(object value)
        => DateOnly.Parse((string)value);
}

internal class TimeOnlyHandler : SqliteTypeHandler<TimeOnly>
{
    public override TimeOnly Parse(object value)
        => TimeOnly.Parse((string)value);
}

internal class TimeSpanHandler : SqliteTypeHandler<TimeSpan>
{
    public override TimeSpan Parse(object value)
        => TimeSpan.Parse((string)value);
}

internal class StringArrayHandler : SqlMapper.TypeHandler<string[]>
{
    public override void SetValue(IDbDataParameter parameter, string[]? value)
    {
        if (value is null)
        {
            parameter.Value = DBNull.Value;
        }
        else
        {
            parameter.Value = JsonSerializer.Serialize(value);
        }
    }

    public override string[]? Parse(object value)
    {
        return value is DBNull ? null : JsonSerializer.Deserialize<string[]>((string)value);
    }
}
