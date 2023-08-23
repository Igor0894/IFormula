using Interpreter.Delegates;
using System.Linq.Expressions;
using System.Reflection;

namespace Interpreter
{
    public class Functions
    {
        double tgt = Convert.ToDouble(0);
        public List<Function> Added = new List<Function>();
        public List<Function> Unadded = new List<Function>();
        private readonly Type[] types = new[] { typeof(Math), typeof(TSDB), typeof(Date), typeof(Operation), typeof(string) };
        private readonly string[] unusedMethods = new[] { "Ceiling", "DivRem", "BigMul", "IEEERemainder",
            "CompareOrdinal", "Copy", "Intern", "IsInterned", "Clone", "op_Equality",  "op_Inequality"};
        private readonly string[] unusedDefenition = new[] { "" };
        public Functions(ref DynamicExpresso.Interpreter interpreter)
        {
            foreach (Type type in types)
            {
                MethodInfo[] arrayMethodInfo = type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);
                for (int i = 0; i < arrayMethodInfo.Length; i++)
                {
                    string name = arrayMethodInfo[i].Name;
                    string definition = MethodDefinition(arrayMethodInfo[i]);
                    if (unusedMethods.Contains(name) || unusedDefenition.Contains(definition)) continue;
                    if(type == typeof(string) && name == "Compare") { continue; }
                    try
                    {
                        interpreter.SetFunction(name, CreateDelegate(arrayMethodInfo[i]));
                    }
                    catch (Exception) { }
                }
            }
        }
        public Functions()
        {
            foreach (Type type in types)
            {
                MethodInfo[] arrayMethodInfo = type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);
                for (int i = 0; i < arrayMethodInfo.Length; i++)
                {
                    string name = arrayMethodInfo[i].Name;
                    string definition = MethodDefinition(arrayMethodInfo[i]);
                    if (unusedMethods.Contains(name) || unusedDefenition.Contains(definition)) continue;
                    Function function = new Function
                    {
                        Name = name,
                        Definition = definition,
                    };
                    try
                    {
                        function.Delegate = CreateDelegate(arrayMethodInfo[i]);
                        Added.Add(function);
                    }
                    catch (Exception e)
                    {
                        function.Exeption = e.Message;
                        Unadded.Add(function);
                    }
                }
            }
        }
        private string MethodDefinition(MethodInfo method)
        {
            return method.Name + "(" +
                String.Join(",", (from parameter in method.GetParameters()
                                  select
                                  parameter.ParameterType.Name + " " + parameter.Name + (parameter.HasDefaultValue ? "=" + parameter.DefaultValue : ""))) +
                ") As " + method.ReturnType.Name;
        }
        private Delegate CreateDelegate(MethodInfo method)
        {
            if (method == null)
                throw new ArgumentNullException("method");
            if (!method.IsStatic)
                throw new ArgumentException("The provided method must be static.", method.Name);
            if (method.IsGenericMethod)
                throw new ArgumentException("The provided method must not be generic.", method.Name);
            return method.CreateDelegate(Expression.GetDelegateType((from parameter in method.GetParameters() select parameter.ParameterType)
                .Concat(new[] { method.ReturnType })
                .ToArray()));
        }
        private Type CreateDelegateType(MethodInfo methodInfo)
        {
            Type[] typeArgs = methodInfo.GetParameters().Select(delegate (ParameterInfo param)
            {
                return param.ParameterType;
            }).Append(methodInfo.ReturnType)
                .ToArray();
            return Expression.GetDelegateType(typeArgs);
        }
    }
}
