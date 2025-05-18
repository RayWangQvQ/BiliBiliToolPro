using System;
using System.Text.RegularExpressions;
using BlazingQuartz.Jobs.Abstractions.Resolvers;
using BlazingQuartz.Jobs.Abstractions.Resolvers.V1;

namespace BlazingQuartz.Jobs.Abstractions.Processors
{
    internal class SystemVariableV1Provider
    {
        char[] separators = new char[] { ' ', '}' };

        private static Dictionary<string, IResolver> Resolvers;

        static SystemVariableV1Provider()
        {
            Resolvers = new()
            {
                { VariableNameContants.DateTime, new DateTimeVariableResolver() },
                { VariableNameContants.LocalDateTime, new LocalDateTimeVariableResolver() },
                { VariableNameContants.Guid, new GuidVariableResolver() },
            };
        }

        public string Resolve(string varBlock)
        {
            var varName = varBlock.Split(separators, 2).First().Substring(2);

            IResolver? resolver;
            if (!Resolvers.TryGetValue(varName, out resolver))
            {
                return varBlock;
            }

            return resolver.Resolve(varBlock);
        }

        /// <summary>
        /// Add variable resolver
        /// </summary>
        /// <param name="key"></param>
        /// <param name="resolver"></param>
        /// <returns>false if not added</returns>
        public static bool AddResolver(string key, IResolver resolver)
        {
            // don't allow overwrite exiting value
            return Resolvers.TryAdd(key, resolver);
        }
    }
}
