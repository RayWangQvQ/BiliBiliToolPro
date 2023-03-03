using System.Collections.Generic;
using AntDesign;

namespace Ray.BiliTool.Blazor.Pages.Account.Settings
{
    public partial class Index
    {
        private readonly Dictionary<string, string> _menuMap = new Dictionary<string, string>
        {
            {"base", "Basic Settings"},
            {"security", "Security Settings"},
            {"binding", "Account Binding"},
            {"notification", "New Message Notification"},
        };

        private string _selectKey = "base";

        private void SelectKey(MenuItem item)
        {
            _selectKey = item.Key;
        }
    }
}