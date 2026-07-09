using System;

namespace Nuri.Runtime.Diagnostics
{
    internal static class DiagnosticsValueFormatter
    {
        public static string TypeName(Type type)
        {
            return type.Name;
        }

        public static string TypeName(object? value)
        {
            return value == null ? "null" : value.GetType().Name;
        }

        public static string Summary(object? value)
        {
            if (value == null)
                return "null";

            if (value is string text)
                return text.Length <= 80 ? text : text.Substring(0, 77) + "...";

            if (value is ValueType)
                return value.ToString() ?? string.Empty;

            return value.GetType().Name;
        }

        public static string DependenciesSummary(object?[]? dependencies)
        {
            return dependencies == null ? "always" : dependencies.Length + " dependencies";
        }
    }
}
