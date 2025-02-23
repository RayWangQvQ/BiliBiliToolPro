using System.ComponentModel;

namespace Ray.BiliBiliTool.Application.Attributes;

public enum TaskLevel
{
    [DefaultValue(5)]
    One,

    [DefaultValue(3)]
    Two,

    [DefaultValue(2)]
    Three,
}