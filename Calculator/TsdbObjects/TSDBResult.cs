namespace Interpreter.TsdbObjects
{
    public class TSDBResult
    {
        public object Value { get; set; } = "NoData";
        public DateTime Time { get; set; } = DateTime.MinValue;
        public string Digital { get; set; } = "NoData";
        public bool Good { get; set; } = false;
    }
}
