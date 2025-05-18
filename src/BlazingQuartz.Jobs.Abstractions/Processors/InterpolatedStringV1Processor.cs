using System;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace BlazingQuartz.Jobs.Abstractions.Processors
{
    public class InterpolatedStringV1Processor
    {
        const string VariableRegex = @"\{{2}(\$.+?)\}{2}";

        private readonly ILogger<InterpolatedStringV1Processor>? _logger;

        public InterpolatedStringV1Processor(ILogger<InterpolatedStringV1Processor>? logger)
        {
            _logger = logger;
        }

        public string? Process(DataMapValue interpolatedString)
        {
            if (interpolatedString.Type != DataMapValueType.InterpolatedString)
                throw new ArgumentException(
                    $"Invalid DataMapValue type {interpolatedString.Type}. Expected type {DataMapValueType.InterpolatedString}."
                );

            if (string.IsNullOrEmpty(interpolatedString.Value))
                return interpolatedString.Value;

            StringBuilder strBldr = new StringBuilder();
            int lastIndex = 0;

            var provider = new SystemVariableV1Provider();

            _logger?.LogDebug(
                "Processing interpolated string [{string}]",
                interpolatedString.Value
            );
            foreach (
                Match match in Regex.Matches(
                    interpolatedString.Value,
                    VariableRegex,
                    RegexOptions.None
                )
            )
            {
                _logger?.LogDebug(
                    "Matched '{matchValue}' on index {index} length {length}.",
                    match.Value,
                    match.Index,
                    match.Length
                );
                strBldr.Append(
                    interpolatedString.Value.Substring(lastIndex, match.Index - lastIndex)
                );
                lastIndex = match.Index + match.Length;

                strBldr.Append(provider.Resolve(match.Value));
            }

            // append remaining
            strBldr.Append(interpolatedString.Value.Substring(lastIndex));

            return strBldr.ToString();
        }
    }
}
