using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace FellsideDigital.Domain.Extensions;

public record EnumOption(string Value, string Display);

public static class EnumExtensions
{
    public static string DisplayName(this Enum value)
    {
        if (value == null)
            return string.Empty;

        var field = value.GetType().GetField(value.ToString());
        if (field == null)
            return value.ToString();

        var attribute = field.GetCustomAttribute<DisplayAttribute>();

        return attribute?.GetName() ?? value.ToString();
    }

    public static IEnumerable<T> GetValues<T>() where T : Enum
    {
        return Enum.GetValues(typeof(T)).Cast<T>();
    }

    public static List<EnumOption> ToOptions<T>() where T : Enum
    {
        return Enum.GetValues(typeof(T))
            .Cast<T>()
            .Select(e => new EnumOption(
                e.ToString(),
                (e as Enum).DisplayName()
            ))
            .ToList();
    }
}
