using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ray.BiliBiliTool.Config
{
    public class EntityChangeEventArgs : EventArgs
    {
        public object Entity { get; }
        public EntityChangeEventArgs(object entity)
        {
            Entity = entity;
        }
    }
}
