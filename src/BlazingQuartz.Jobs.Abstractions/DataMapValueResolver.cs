using System;
using BlazingQuartz.Jobs.Abstractions.Processors;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BlazingQuartz.Jobs.Abstractions
{
    public class DataMapValueResolver : IDataMapValueResolver
    {
        private readonly IServiceProvider? _svcProvider;

        public DataMapValueResolver(IServiceProvider? svcProvider)
        {
            _svcProvider = svcProvider;
        }

        public string? Resolve(DataMapValue? dmv)
        {
            if (dmv == null)
                return null;

            switch (dmv.Type)
            {
                case DataMapValueType.InterpolatedString:
                    switch (dmv.Version)
                    {
                        case 1:
                            var logger = _svcProvider?.GetRequiredService<
                                ILogger<InterpolatedStringV1Processor>
                            >();
                            var processor = new InterpolatedStringV1Processor(logger);
                            return processor.Process(dmv);
                        default:
                            throw new NotSupportedException(
                                $"DataMapValue {dmv.Type} version {dmv.Version} is not supported."
                            );
                    }
                default:
                    throw new NotSupportedException(
                        $"DataMapValueType {dmv.Type} is not supported."
                    );
            }
        }
    }
}
