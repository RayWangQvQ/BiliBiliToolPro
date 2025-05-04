using System;

namespace BlazingQuartz.Core.Models
{
    public class PagedList<T> : List<T>
    {
        public PageMetadata? PageMetadata { get; set; }

        public PagedList(IEnumerable<T> collection)
            : this(collection, null) { }

        public PagedList(IEnumerable<T> collection, PageMetadata? metadata)
            : base(collection)
        {
            PageMetadata = metadata;
        }
    }

    public record PageMetadata
    {
        /// <summary>
        /// Page number. Start at 0
        /// </summary>
        public int Page { get; init; } = 0;

        /// <summary>
        /// Total number of records
        /// </summary>
        public int TotalCount { get; init; }

        /// <summary>
        /// Max number of records per page
        /// </summary>
        public int PageSize { get; init; } = 500;

        public PageMetadata(int Page, int PageSize)
        {
            this.Page = Page;
            this.PageSize = PageSize;
        }
    }
}
