using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebApiClientCore;
using WebApiClientCore.Attributes;

namespace Ray.BiliBiliTool.Agent.Attributes
{
    public class AddIfNotExistHeaderAttribute : HeaderAttribute
    {
        private readonly string _name;
        private readonly string _value;

        public AddIfNotExistHeaderAttribute(string name, string value) : base(name, value)
        {
            this._name = name;
            this._value = value;
        }

        public AddIfNotExistHeaderAttribute(string aliasName) : base(aliasName)
        {

        }

        public override Task OnRequestAsync(ApiRequestContext context)
        {
            //因为逻辑是先添加子类的，再加基类的。所以当子类已经添加过了，基类就不加了
            if (context.HttpContext.RequestMessage.Headers.Contains(this._name))
                return Task.CompletedTask;

            return base.OnRequestAsync(context);
        }
    }
}
