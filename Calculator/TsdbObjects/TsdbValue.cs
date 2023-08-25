using System.Diagnostics.Metrics;

namespace Interpreter.TsdbObjects
{
    public class TsdbValue
    {
        public object Value { get; set; } = "NoData";
        public DateTime Time { get; set; } = DateTime.MinValue;
        public string Digital { get; set; } = "NoData";
        public bool Good { get; set; } = false;
        public override string ToString()
        {
            return Value.ToString();
        }
        public static double operator +(TsdbValue result1, TsdbValue result2)
        {
            return double.Parse(result1.Value.ToString()) + double.Parse(result2.Value.ToString());
        }
        public static double operator -(TsdbValue result1, TsdbValue result2)
        {
            return double.Parse(result1.Value.ToString()) - double.Parse(result2.Value.ToString());
        }
        public static double operator *(TsdbValue result1, TsdbValue result2)
        {
            return double.Parse(result1.Value.ToString()) * double.Parse(result2.Value.ToString());
        }
        public static double operator /(TsdbValue result1, TsdbValue result2)
        {
            return double.Parse(result1.Value.ToString()) / double.Parse(result2.Value.ToString());
        }
        public static bool operator >=(TsdbValue result1, TsdbValue result2)
        {
            return double.Parse(result1.Value.ToString()) >= double.Parse(result2.Value.ToString());
        }
        public static bool operator <=(TsdbValue result1, TsdbValue result2)
        {
            return double.Parse(result1.Value.ToString()) <= double.Parse(result2.Value.ToString());
        }
        public static double operator +(TsdbValue result1, double result2)
        {
            return double.Parse(result1.Value.ToString()) + result2;
        }
        public static double operator -(TsdbValue result1, double result2)
        {
            return double.Parse(result1.Value.ToString()) - result2;
        }
        public static double operator *(TsdbValue result1, double result2)
        {
            return double.Parse(result1.Value.ToString()) * result2;
        }
        public static double operator /(TsdbValue result1, double result2)
        {
            return double.Parse(result1.Value.ToString()) / result2;
        }
        public static bool operator >=(TsdbValue result1, double result2)
        {
            return double.Parse(result1.Value.ToString()) >= result2;
        }
        public static bool operator <=(TsdbValue result1, double result2)
        {
            return double.Parse(result1.Value.ToString()) <= result2;
        }
        public static double operator +(double result1, TsdbValue result2)
        {
            return result1 + double.Parse(result2.Value.ToString());
        }
        public static double operator -(double result1, TsdbValue result2)
        {
            return result1 - double.Parse(result2.Value.ToString());
        }
        public static double operator *(double result1, TsdbValue result2)
        {
            return result1 * double.Parse(result2.Value.ToString());
        }
        public static double operator /(double result1, TsdbValue result2)
        {
            return result1 / double.Parse(result2.Value.ToString());
        }
        public static bool operator >=(double result1, TsdbValue result2)
        {
            return result1 >= double.Parse(result2.Value.ToString());
        }
        public static bool operator <=(double result1, TsdbValue result2)
        {
            return result1 <= double.Parse(result2.Value.ToString());
        }
    }
}
