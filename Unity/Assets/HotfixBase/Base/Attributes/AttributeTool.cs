public class AttributeTool
{
    public static T GetAttribute<T>(System.Reflection.ICustomAttributeProvider att)
    {
        var t = typeof(T);
        foreach (var ator in att.GetCustomAttributes(t, false))
            return (T) ator;

        return default(T);
    }
}