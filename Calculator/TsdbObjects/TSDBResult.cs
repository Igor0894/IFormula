using System.Diagnostics.Metrics;

namespace Interpreter.TsdbObjects
{
    public class TSDBResult
    {
        public object Value { get; set; } = "NoData";
        public DateTime Time { get; set; } = DateTime.MinValue;
        public string Digital { get; set; } = "NoData";
        public bool Good { get; set; } = false;
        public override string ToString()
        {
            return Value.ToString();
        }
        public static double operator +(TSDBResult result1, TSDBResult result2)
        {
            return double.Parse(result1.Value.ToString()) + double.Parse(result2.Value.ToString());
        }
        public static double operator -(TSDBResult result1, TSDBResult result2)
        {
            return double.Parse(result1.Value.ToString()) - double.Parse(result2.Value.ToString());
        }
        public static double operator *(TSDBResult result1, TSDBResult result2)
        {
            return double.Parse(result1.Value.ToString()) * double.Parse(result2.Value.ToString());
        }
        public static double operator /(TSDBResult result1, TSDBResult result2)
        {
            return double.Parse(result1.Value.ToString()) / double.Parse(result2.Value.ToString());
        }
        public static bool operator >=(TSDBResult result1, TSDBResult result2)
        {
            return double.Parse(result1.Value.ToString()) >= double.Parse(result2.Value.ToString());
        }
        public static bool operator <=(TSDBResult result1, TSDBResult result2)
        {
            return double.Parse(result1.Value.ToString()) <= double.Parse(result2.Value.ToString());
        }
    }
}
