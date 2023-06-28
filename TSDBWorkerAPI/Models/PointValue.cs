namespace TSDBWorkerAPI.Models
{

    public class PointValue
    {
        public DateTime Timestamp;
        public double Value;
        public PointValue(DateTime Timestamp, double Value)
        {
            this.Timestamp = Timestamp;
            this.Value = Value;
        }
        public override string ToString()
        {
            return $"{Timestamp} {Value}";
        }
        public static DateTime Round10minsTS(DateTime TS)
        {
            /*
             *  На среде ГПН сбор данных производится раз в 10 минут.
             *  Пока приутствует проблема в интерфейсе OPC UA -> TSDB со сдвигом
             *  мы вынуждены округлять до десятка минут (на Linux).
             *  На тестовой среде у нас сбор раз в минуту.
             */
            //  return TS;
            /*
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return TS;
            }
            else*/
            return new DateTime(TS.Year, TS.Month, TS.Day, TS.Hour, Convert.ToInt16((TS.Minute / 10).ToString() + "0"), 0);


        }
    }
    public class PointValueWithQuality : PointValue
    {
        public Quality quality;
        public PointValueWithQuality(DateTime Timestamp, double Value) : base(Timestamp, Value)
        {
            quality = Quality.good;
        }
        public PointValueWithQuality(DateTime Timestamp, double Value, Quality Quality) : base(Timestamp, Value)
        {
            this.Timestamp = Timestamp;
            this.Value = Value;
            this.quality = Quality;
        }
        public PointValue Convert2PointValue()
        {
            return new PointValue(this.Timestamp, this.Value);
        }
        public override string ToString()
        {
            return $"{Timestamp} {Value} {quality}";
        }
    }
}
