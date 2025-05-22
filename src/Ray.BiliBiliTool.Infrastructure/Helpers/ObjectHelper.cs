using System.Reflection;

namespace Ray.BiliBiliTool.Infrastructure.Helpers;

public static class ObjectHelper
{
    public static Dictionary<string, object?> ObjectToDictionary(object obj)
    {
        // 获取对象的所有属性
        PropertyInfo[] properties = obj.GetType().GetProperties();

        // 遍历所有属性并将其添加到字典中
        return properties.ToDictionary(
            property => property.Name,
            property => property.GetValue(obj)
        );
    }
}
