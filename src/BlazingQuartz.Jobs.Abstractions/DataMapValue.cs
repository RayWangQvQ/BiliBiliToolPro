using System;
using System.Globalization;
using System.Text.Json;

namespace BlazingQuartz.Jobs.Abstractions
{
    public class DataMapValue
    {
        public DataMapValueType Type { get; set; }
        public string? Value { get; set; }
        public int Version { get; set; }

        public DataMapValue()
            : this(DataMapValueType.InterpolatedString, 1) { }

        public DataMapValue(DataMapValueType type, int version)
            : this(type, null, version) { }

        public DataMapValue(DataMapValueType type, string? value = null, int version = 1)
        {
            Type = type;
            Value = value;
            Version = version;
        }

        public override string ToString()
        {
            return JsonSerializer.Serialize(this);
        }

        /// <summary>
        /// Create DataMapValue instance based from specified value
        /// </summary>
        /// <param name="dataMapValue"></param>
        /// <returns></returns>
        public static DataMapValue? Create(object? dataMapValue)
        {
            var value = Convert.ToString(dataMapValue, CultureInfo.InvariantCulture);
            if (value == null)
                return null;

            return JsonSerializer.Deserialize<DataMapValue>(value);
        }

        /// <summary>
        /// Create DataMapValue instance based from specified value
        /// </summary>
        /// <param name="dataMapValue"></param>
        /// <returns></returns>
        public static DataMapValue? Create(string? dataMapValue)
        {
            if (dataMapValue == null)
                return null;

            return JsonSerializer.Deserialize<DataMapValue>(dataMapValue);
        }

        public static DataMapValue Create(
            object? dataMapValue,
            DataMapValueType defaultType,
            int defaultVersion,
            string? defaultValue = null
        )
        {
            var dmv = Create(dataMapValue);
            if (dmv != null)
                return dmv;
            else
                return new(defaultType, defaultValue, defaultVersion);
        }
    }
}
