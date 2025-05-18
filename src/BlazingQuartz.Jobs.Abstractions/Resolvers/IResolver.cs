using System;

namespace BlazingQuartz.Jobs.Abstractions.Resolvers
{
    public interface IResolver
    {
        string Resolve(string varBlock);
    }
}
