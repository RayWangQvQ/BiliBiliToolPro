using System.Collections.Generic;
using Ray.BiliTool.Blazor.Web.Models;
using Microsoft.AspNetCore.Components;

namespace Ray.BiliTool.Blazor.Web.Pages.Account.Center
{
    public partial class Articles
    {
        [Parameter] public IList<ListItemDataType> List { get; set; }
    }
}