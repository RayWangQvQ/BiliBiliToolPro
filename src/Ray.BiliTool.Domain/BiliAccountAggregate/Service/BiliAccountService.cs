using Ray.BiliTool.Domain.BiliAccountAggregate.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ray.DDD;

namespace Ray.BiliTool.Domain.BiliAccountAggregate.Service
{
    public class BiliAccountService: IDomainService
    {
        private readonly IBiliAccountRepository _repository;

        public BiliAccountService(
            IBiliAccountRepository repository
            )
        {
            _repository = repository;
        }

        public void LoginByScan()
        {

        }
    }
}
