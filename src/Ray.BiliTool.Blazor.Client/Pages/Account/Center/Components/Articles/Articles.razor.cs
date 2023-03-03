using System.Collections.Generic;
using Ray.BiliTool.Blazor.Models;
using Microsoft.AspNetCore.Components;

namespace Ray.BiliTool.Blazor.Pages.Account.Center
{
    public partial class Articles
    {
        [Parameter] public IList<ListItemDataType> List { get; set; }
    }
}