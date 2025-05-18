using System;

namespace BlazingQuartz.Jobs.Abstractions.Resolvers.V1
{
    internal class LocalDateTimeVariableResolver : DateTimeVariableResolver
    {
        const string DatetimeRegex =
            $"\\{VariableNameContants.LocalDateTime}\\s(rfc1123|iso8601|\'.+\'|\\\".+\\\")(?:\\s(\\-?\\d+)\\s(y|M|d|h|m|s|ms))?";

        internal override string GetVariableName()
        {
            return VariableNameContants.LocalDateTime;
        }

        internal override string GetVariableRegex()
        {
            return DatetimeRegex;
        }

        internal override DateTimeOffset GetDateTimeOffset()
        {
            return DateTimeOffset.Now;
        }
    }
}
