using System;
using System.Collections.Generic;
using System.Text;

namespace BiliBiliTool.Agent
{
    public class ApiResponse : ApiResponse<object>
    {

    }

    public class ApiResponse<TData>
    {
        public int Code { get; set; } = int.MinValue;

        public string Message { get; set; }

        public TData Data { get; set; }
    }
}
