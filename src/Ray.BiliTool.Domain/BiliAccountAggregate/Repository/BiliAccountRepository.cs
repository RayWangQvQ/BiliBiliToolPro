using Ray.BiliTool.Domain.BiliAccountAggregate.Entity;
using Ray.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ray.BiliTool.Domain.BiliAccountAggregate.Repository
{
    public interface IBiliAccountRepository: IBaseRepository<BiliAccount,long>
    {

    }
}
