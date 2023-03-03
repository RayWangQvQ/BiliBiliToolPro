using System.Collections.Generic;
using AntDesign.ProLayout;
using Microsoft.AspNetCore.Components;

namespace Ray.BiliTool.Blazor.Pages.List
{
    public partial class SearchList
    {
        private readonly IList<TabPaneItem> _tabList = new List<TabPaneItem>
        {
            new TabPaneItem {Key = "articles", Tab = "Articles"},
            new TabPaneItem {Key = "projects", Tab = "Projects"},
            new TabPaneItem {Key = "applications", Tab = "Applications"}
        };

        [Inject] protected NavigationManager NavigationManager { get; set; }

        private string GetTabKey()
        {
            var url = NavigationManager.Uri.TrimEnd('/');
            var key = url.Substring(url.LastIndexOf('/') + 1);
            return key;
        }

        private void HandleTabChange(string key)
        {
            var url = NavigationManager.Uri.TrimEnd('/');
            url = url.Substring(0, url.LastIndexOf('/'));
            NavigationManager.NavigateTo($"{url}/{key}");
        }
    }
}