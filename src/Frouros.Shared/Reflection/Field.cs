using System.Reflection;
using System.Reflection.Emit;

namespace Frouros.Shared.Reflection;

public static class Field
{
    public static Func<TType, TField> CreateSetter<TType, TField>(this FieldInfo field)
    {
        var name = $"{field.ReflectedType?.FullName}.set_{field.Name}";

        var method = new DynamicMethod(name, null, new[]
        {
            typeof(TType),
            typeof(TField)
        }, true);

        var gen = method.GetILGenerator();
        
        gen.Emit(OpCodes.Ldarg_0);
        gen.Emit(OpCodes.Ldarg_1);
        gen.Emit(OpCodes.Ldfld);
        gen.Emit(OpCodes.Ret);

        return method.CreateDelegate<Func<TType, TField>>();
    }
}