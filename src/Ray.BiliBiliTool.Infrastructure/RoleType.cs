using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ray.BiliBiliTool.Infrastructure
{
    public enum RoleType
    {
        [Display(Name = "无")]
        Non,
        [Display(Name = "超管")]
        Admin,
        [Display(Name = "管理员")]
        Manager,
        [Display(Name = "普通用户")]
        Guest
    }
}
