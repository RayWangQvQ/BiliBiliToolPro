using System;

namespace BlazingQuartz.Jobs.Abstractions
{
    public interface IJobUI
    {
        string JobClass { get; }

        bool IsReadOnly { get; set; }

        IDictionary<string, object> JobDataMap { get; set; }

        /// <summary>
        /// Remove all the JobUI keys that was added to JobDataMap
        /// </summary>
        /// <returns></returns>
        Task ClearChanges();

        /// <summary>
        /// Apply the changes to JobDataMap
        /// </summary>
        /// <returns>true if no validation error and all changes applied</returns>
        Task<bool> ApplyChanges();
    }
}
