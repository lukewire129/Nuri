using System;
using System.Globalization;

namespace Nuri.UI.Values
{
    internal static class GridLengthParser
    {
        public static LengthValue[] Parse(string definitions, string parameterName)
        {
            if (definitions == null)
                throw new ArgumentNullException(parameterName);

            if (string.IsNullOrWhiteSpace(definitions))
                throw new FormatException("Grid length definitions cannot be empty.");

            var tokens = definitions.Split(',');
            var values = new LengthValue[tokens.Length];

            for (var index = 0; index < tokens.Length; index++)
                values[index] = ParseToken(tokens[index], index);

            return values;
        }

        private static LengthValue ParseToken(string rawToken, int index)
        {
            var token = rawToken.Trim();
            if (token.Length == 0)
                throw InvalidToken(token, index);

            if (string.Equals(token, "Auto", StringComparison.OrdinalIgnoreCase))
                return LengthValue.Auto();

            if (token.EndsWith("*", StringComparison.Ordinal))
            {
                var weightToken = token.Substring(0, token.Length - 1);
                if (weightToken.Length == 0)
                    return LengthValue.Star();

                if (TryParseNumber(weightToken, out var weight))
                    return LengthValue.Star(weight);

                throw InvalidToken(token, index);
            }

            if (TryParseNumber(token, out var pixels))
                return LengthValue.Pixels(pixels);

            throw InvalidToken(token, index);
        }

        private static bool TryParseNumber(string value, out double result)
        {
            return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result);
        }

        private static FormatException InvalidToken(string token, int index)
        {
            return new FormatException($"Invalid grid length token at index {index}: '{token}'.");
        }
    }
}
