using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ray.BiliBiliTool.Config
{
    public class EntityChangeObserver
    {
        public event EventHandler<EntityChangeEventArgs> Changed;

        public void OnChanged(EntityChangeEventArgs e)
        {
            ThreadPool.QueueUserWorkItem((_) => Changed?.Invoke(this, e));
        }

        #region singleton

        private static readonly Lazy<EntityChangeObserver> lazy
            = new Lazy<EntityChangeObserver>(() => new EntityChangeObserver());

        private EntityChangeObserver() { }

        public static EntityChangeObserver Instance => lazy.Value;

        #endregion singleton
    }
}
