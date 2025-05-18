using System;
using System.Text.RegularExpressions;

namespace BlazingQuartz.Jobs.Abstractions.Resolvers.V1
{
    internal class DateTimeVariableResolver : IResolver
    {
        const string DatetimeRegex =
            $"\\{VariableNameContants.DateTime}\\s(rfc1123|iso8601|\'.+\'|\\\".+\\\")(?:\\s(\\-?\\d+)\\s(y|M|d|h|m|s|ms))?";

        public string Resolve(string varBlock)
        {
            var result = Regex.Match(varBlock, GetVariableRegex());

            if (!result.Success || result.Index != 2)
                throw new FormatException(
                    $"Invalid {GetVariableName()} format. Expected format is "
                        + "{{$datetime rfc1123|iso8601|'date format'|\"date format\" [integer y|M|w|d|h|m|s|ms]}}."
                );

            var dt = GetDateTimeOffset();

            var format = result.Groups[1].Value;
            var rawOffset = result.Groups[2].Value;
            if (!string.IsNullOrEmpty(rawOffset))
            {
                int offset;
                if (!int.TryParse(rawOffset, out offset))
                {
                    throw new FormatException(
                        $"Invalid {GetVariableName()} pattern. Offset '{rawOffset}' should be numeric value."
                    );
                }
                var offsetOption = result.Groups[3].Value;

                switch (offsetOption)
                {
                    case "y":
                        dt = dt.AddYears(offset);
                        break;
                    case "M":
                        dt = dt.AddMonths(offset);
                        break;
                    case "d":
                        dt = dt.AddDays(offset);
                        break;
                    case "h":
                        dt = dt.AddHours(offset);
                        break;
                    case "m":
                        dt = dt.AddMinutes(offset);
                        break;
                    case "s":
                        dt = dt.AddSeconds(offset);
                        break;
                    case "ms":
                        dt = dt.AddMilliseconds(offset);
                        break;
                    default:
                        throw new FormatException(
                            $"Invalid {GetVariableName()} pattern. Need to provide valid offset option."
                        );
                }
            }

            switch (format)
            {
                case "rfc1123":
                    return dt.ToString("r");
                case "iso8601":
                    return dt.ToString("u");
                default:
                    // value was enclosed in quotes, ignore first and last character
                    return dt.ToString(format.Substring(1, format.Length - 2));
            }
        }

        internal virtual string GetVariableRegex()
        {
            return DatetimeRegex;
        }

        internal virtual string GetVariableName()
        {
            return VariableNameContants.DateTime;
        }

        internal virtual DateTimeOffset GetDateTimeOffset()
        {
            return DateTimeOffset.UtcNow;
        }
    }
}
