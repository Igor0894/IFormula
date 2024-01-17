using IspAttribute = ISP.SDK.IspObjects.Attribute;

namespace Interpreter
{
    public class Variable
    {
        public object Value { get; }
        public VariableValueType ValueType { get; }
        public Variable(object value, VariableValueType valueType)
        {
            Value = value;
            ValueType = valueType;
        }
        public Variable(IspAttribute ispAttribute)
        { 
            Value = ispAttribute.Value;
            ValueType = (VariableValueType)Enum.Parse(typeof(VariableValueType), ispAttribute.ValueType.ToString());
        }
    }
    public enum VariableValueType
    {
        PI,
        SQL,
        Static,
        Calculation,
        IHistorian,
        TSDB
    }
}
