﻿using Interpreter.TsdbObjects;
using TSDBWorkerAPI.Models;
using TSDBWorkerAPI;

namespace Interpreter.Delegates
{
    public static class TSDB
#nullable disable
    {
        public static TsdbClient TsdbClient;
        internal static Dictionary<string, Value_Type> InputTagsTypes = new Dictionary<string, Value_Type>();
        internal static Mutex InputTagsTypesMutex = new Mutex();
        static readonly Dictionary<string, RetrievalTypeConstants> methods = new Dictionary<string, RetrievalTypeConstants>
        {
            {"exactorprev", RetrievalTypeConstants.rtAtOrBefore },
            {"exactornext", RetrievalTypeConstants.rtAtOrAfter },
            {"next", RetrievalTypeConstants.rtAfter },
            {"prev", RetrievalTypeConstants.rtBefore }
        };
        public static async Task UpdateValue(IDictionary<string, Value_Type> TagTypes, string tagName, object value, DateTime dateTime)
        {
            /*if (!DateTime.TryParse(dateTime.ToString(), out DateTime ts))
                throw new Exception("Неверный формат даты");*/
            Dictionary<string, List<TSDBSimpleValue>> values = new Dictionary<string, List<TSDBSimpleValue>> { };
            TSDBSimpleValue valPoint = new TSDBSimpleValue(){
                Value = value,
                TimestampUTC = dateTime.ToUniversalTime()
            };
            values.TryAdd(tagName, new List<TSDBSimpleValue> { valPoint });
            Value_Type tagType = TagTypes[tagName];
            if (tagType == Value_Type.SET && long.TryParse(value.ToString(), out long resultLong))
            {
                await TsdbClient.WriteLongVals(values);
            }
            else if (tagType == Value_Type.FLOAT && float.TryParse(value.ToString(), out float resultFloat))
            {
                await TsdbClient.WriteFloatVals(values);
            }
            else if (tagType == Value_Type.DOUBLE && double.TryParse(value.ToString(), out double resultDouble))
            {
                await TsdbClient.WriteDoubleVals(values);
            }
            else
            {
                await TsdbClient.WriteStringVals(values);
            }
        }
        public static async Task UpdateValues<T>(IDictionary<string, List<TSDBSimpleValue>> valuesForWrite)
        {
            if(typeof(T) == typeof(double))
            {
                await TsdbClient.WriteDoubleVals(valuesForWrite);
            }
            else if (typeof(T) == typeof(long))
            {
                await TsdbClient.WriteLongVals(valuesForWrite);
            }
            else if (typeof(T) == typeof(float))
            {
                await TsdbClient.WriteFloatVals(valuesForWrite);
            }
            else
            {
                await TsdbClient.WriteStringVals(valuesForWrite);
            }
        }
        public static TSDBResult TagVal(string tagName, object timeStamp = null, string method = "ExactOrPrev")
        {
            if (string.IsNullOrEmpty(tagName)) { throw new Exception("Не указано имя тега"); }
            if(!methods.ContainsKey(method.ToLower())) { throw new Exception($"Не распознан метод получения значения: {method}"); }
            TSDBResult tsdbesult = new TSDBResult();
            DateTime ts =DateTime.MinValue;
            if (timeStamp is null || !DateTime.TryParse(timeStamp.ToString(), out ts))
                throw new Exception("Неверный формат даты");
            Dictionary<string, List<TSDBValue>> tsdbValues = GetTSDBValues(tagName, ts, method);
            if (tsdbValues.Values.Count > 0 && tsdbValues.Values.FirstOrDefault().Count > 0)
            {
                TSDBValue tsdbValue = tsdbValues.Values.FirstOrDefault()[0];
                tsdbesult.Value = tsdbValue.Value;
                tsdbesult.Digital = tsdbValue.DigitalSetValue;
                tsdbesult.Time = tsdbValue.Timestamp;
                tsdbesult.Good = tsdbValue.Quality == Quality.good ? true : false;
                return tsdbesult;
            }
            else
            {
                throw new Exception($"Отсутствуют значения тега {tagName} на метку времени {timeStamp} методом поиска {method}");
            }
        }
        public static TAGInfo TagInfo(string tagName)
        {
            TAGInfo tsdbesult = new TAGInfo
            {
                Desc = TsdbClient.GetDescription(tagName).Result,
                Span = int.Parse(TsdbClient.GetMetaAttribute(tagName, "Span").Result),
                Zero = int.Parse(TsdbClient.GetMetaAttribute(tagName, "Zero").Result),
                EU = TsdbClient.GetMetaAttribute(tagName, "EngUnits").Result,
                InstrumentTag = TsdbClient.GetMetaAttribute(tagName, "InstrumentTag").Result
            };
            return tsdbesult;
        }
        public static bool BadVal(string tagName, object timeStamp = null, string method = "ExactOrPrev")
        {
            DateTime ts = DateTime.MinValue;
            if (timeStamp is null)
                throw new Exception("Неверный формат даты");
            else
            {
                if (!DateTime.TryParse(timeStamp.ToString(), out ts))
                    throw new Exception("Неверный формат даты");
            }
            TSDBValue tsdbValue = GetTSDBValues(tagName, ts, method).Values.FirstOrDefault()[0];
            return tsdbValue.Quality != Quality.good;
        }
        public static double TagTot(string tagName, object startTime, object endTime, string calculationBasis = "time", int factor = 24)
        {
            if (!DateTime.TryParse(startTime.ToString(), out DateTime st))
                throw new Exception("Неверный формат даты");
            if (!DateTime.TryParse(endTime.ToString(), out DateTime et))
                throw new Exception("Неверный формат даты");
            double total = 0;
            total = Convert.ToDouble(TsdbClient.Summary(tagName, st, et, SummaryType.Total,
                    calculationBasis.ToLower() == "event" ? CalculationBasis.EventWeighted : CalculationBasis.TimeWeighted).Result.Value) * factor;
            return total;
        }
        public static double TagAvg(string tagName, object startTime, object endTime, string calculationBasis = "event")
        {
            if (!DateTime.TryParse(startTime.ToString(), out DateTime st))
                throw new Exception("Неверный формат даты");
            if (!DateTime.TryParse(endTime.ToString(), out DateTime et))
                throw new Exception("Неверный формат даты");
            double average = double.NaN;
            average = Convert.ToDouble(TsdbClient.Summary(tagName, st, et, SummaryType.Average,
                    calculationBasis.ToLower() == "event" ? CalculationBasis.EventWeighted : CalculationBasis.TimeWeighted).Result.Value);
            return average;
        }
        public static double TagMax(string tagName, object startTime, object endTime, string calculationBasis = "event")
        {
            if (!DateTime.TryParse(startTime.ToString(), out DateTime st))
                throw new Exception("Неверный формат даты");
            if (!DateTime.TryParse(endTime.ToString(), out DateTime et))
                throw new Exception("Неверный формат даты");
            double total = 0;
            total = Convert.ToDouble(TsdbClient.Summary(tagName, st, et, SummaryType.Maximum,
                    calculationBasis.ToLower() == "event" ? CalculationBasis.EventWeighted : CalculationBasis.TimeWeighted).Result.Value);
            return total;
        }
        public static double TagMin(string tagName, object startTime, object endTime, string calculationBasis = "event")
        {
            if (!DateTime.TryParse(startTime.ToString(), out DateTime st))
                throw new Exception("Неверный формат даты");
            if (!DateTime.TryParse(endTime.ToString(), out DateTime et))
                throw new Exception("Неверный формат даты");
            double total = 0;
            total = Convert.ToDouble(TsdbClient.Summary(tagName, st, et, SummaryType.Minimum,
                    calculationBasis.ToLower() == "event" ? CalculationBasis.EventWeighted : CalculationBasis.TimeWeighted).Result.Value);
            return total;
        }
        public static double TagCount(string tagName, object startTime, object endTime, string calculationBasis = "event")
        {
            if (!DateTime.TryParse(startTime.ToString(), out DateTime st))
                throw new Exception("Неверный формат даты");
            if (!DateTime.TryParse(endTime.ToString(), out DateTime et))
                throw new Exception("Неверный формат даты");
            double total = 0;
            total = Convert.ToDouble(TsdbClient.Summary(tagName, st, et, SummaryType.Count,
                    calculationBasis.ToLower() == "event" ? CalculationBasis.EventWeighted : CalculationBasis.TimeWeighted).Result.Value);
            return total;
        }
        private static Value_Type GetValueType(string tagName)
        {
            if (!InputTagsTypes.ContainsKey(tagName)) 
            {
                lock (InputTagsTypesMutex)
                {
                    string typeString = TsdbClient.GetMetaAttribute(tagName, "Value_Type").Result;
                    Value_Type type = (Value_Type)Enum.Parse(typeof(Value_Type), typeString);
                    InputTagsTypes.Add(tagName, type);
                    return type;
                }
            }
            else
            {
                return InputTagsTypes[tagName];
            }
        }
        private static Dictionary<string, List<TSDBValue>> GetTSDBValues(string tagName, DateTime ts, string method)
        {
            Value_Type tagType = GetValueType(tagName);
            Task<Dictionary<string, List<TSDBValue>>> result;
            switch (tagType)
            {
                case Value_Type.DOUBLE:
                    result = TsdbClient.GetArcValue<double>(tagName, ts, methods[method.ToLower()]);
                    break;
                case Value_Type.LONG:
                    result = TsdbClient.GetArcValue<long>(tagName, ts, methods[method.ToLower()]);
                    break;
                case Value_Type.SET:
                    result = TsdbClient.GetArcValue<long>(tagName, ts, methods[method.ToLower()]);
                    break;
                case Value_Type.FLOAT:
                    result = TsdbClient.GetArcValue<float>(tagName, ts, methods[method.ToLower()]);
                    break;
                default:
                    result = TsdbClient.GetArcValue<string>(tagName, ts, methods[method.ToLower()]);
                    break;
            }
            return result.Result;
        }

#warning Требуется переписать методы
        //Первое значение равное Value
        /*public static TSDBResult FindEQ(string tagName, object startTime, object endTime, object value)
        {
            TSDBPoint pt = TSDB.GetTagByName(tagName);
            TSDBResult tsdbesult = new TSDBResult();
            bool isDouble = double.TryParse(value.ToString(), out _);
            if (!DateTime.TryParse(startTime.ToString(), out DateTime st))
                throw new Exception("Неверный формат даты");
            if (!DateTime.TryParse(endTime.ToString(), out DateTime et))
                throw new Exception("Неверный формат даты");
            int count = Convert.ToInt32(pt.Data.Summary(st, et, TSDBSummaryType.Count, TSDBSummaryCalculationBasis.EventWeighted).Value);
            if (count > 0)
            {
                TSDBValue result = pt.Data.RecordedValues(new DateTime(st.Ticks, DateTimeKind.Local), new DateTime(et.Ticks, DateTimeKind.Local), BoundaryTypeConstants.btInside)
                    .Where(item =>
                    {
                        if (isDouble)
                            if (double.TryParse(item.Value.ToString(), out double val)) return val >= Double.Parse(value.ToString());
                        return value.ToString() == item.Value.ToString();
                    }).DefaultIfEmpty(new TSDBValue(pt.Name, DateTime.MinValue, "NoData", String.Empty, Quality.bad)).First();
                tsdbesult.Value = result.Value;
                tsdbesult.Digital = pt.ValueType == TSDBValueType.SET ? result.DigitalSetValue : result.Value.ToString();
                tsdbesult.Time = result.TimestampUTC.ToLocalTime();
                tsdbesult.Good = result.IsGood();
            }
            return tsdbesult;
        }
        //Первое значение равное или больше Value
        public static TSDBResult FindGE(string tagName, object startTime, object endTime, double value)
        {
            TSDBPoint pt = TSDB.GetTagByName(tagName);
            TSDBResult tsdbesult = new TSDBResult();
            if (!DateTime.TryParse(startTime.ToString(), out DateTime st))
                throw new Exception("Неверный формат даты");
            if (!DateTime.TryParse(endTime.ToString(), out DateTime et))
                throw new Exception("Неверный формат даты");
            int count = Convert.ToInt32(pt.Data.Summary(st, et, TSDBSummaryType.Count, TSDBSummaryCalculationBasis.EventWeighted).Value);
            if (count > 0)
            {
                TSDBValue result = pt.Data.RecordedValues(new DateTime(st.Ticks, DateTimeKind.Local), new DateTime(et.Ticks, DateTimeKind.Local), BoundaryTypeConstants.btInside)
                    .Where(item =>
                    {
                        if (double.TryParse(item.Value.ToString(), out double val)) return val >= value;
                        return false;
                    }).DefaultIfEmpty(new TSDBValue(pt.Name, DateTime.MinValue, "NoData", String.Empty, Quality.bad)).First();
                tsdbesult.Value = result.Value;
                tsdbesult.Digital = pt.ValueType == TSDBValueType.SET ? result.DigitalSetValue : result.Value.ToString();
                tsdbesult.Time = result.TimestampUTC.ToLocalTime();
                tsdbesult.Good = result.IsGood();
            }
            return tsdbesult;
        }
        //Первое значение больше Value
        public static TSDBResult FindGT(string tagName, object startTime, object endTime, double value)
        {
            TSDBPoint pt = TSDB.GetTagByName(tagName);
            TSDBResult tsdbesult = new TSDBResult();
            if (!DateTime.TryParse(startTime.ToString(), out DateTime st))
                throw new Exception("Неверный формат даты");
            if (!DateTime.TryParse(endTime.ToString(), out DateTime et))
                throw new Exception("Неверный формат даты");
            int count = Convert.ToInt32(pt.Data.Summary(st, et, TSDBSummaryType.Count, TSDBSummaryCalculationBasis.EventWeighted).Value);
            if (count > 0)
            {
                TSDBValue result = pt.Data.RecordedValues(new DateTime(st.Ticks, DateTimeKind.Local), new DateTime(et.Ticks, DateTimeKind.Local), BoundaryTypeConstants.btInside)
                    .Where(item =>
                    {
                        if (double.TryParse(item.Value.ToString(), out double val)) return val > value;
                        return false;
                    }).DefaultIfEmpty(new TSDBValue(pt.Name, DateTime.MinValue, "NoData", String.Empty, Quality.bad)).First();
                tsdbesult.Value = result.Value;
                tsdbesult.Digital = pt.ValueType == TSDBValueType.SET ? result.DigitalSetValue : result.Value.ToString();
                tsdbesult.Time = result.TimestampUTC.ToLocalTime();
                tsdbesult.Good = result.IsGood();
            }
            return tsdbesult;
        }
        //Первое значение равное или меньше Value
        public static TSDBResult FindLE(string tagName, object startTime, object endTime, double value)
        {
            TSDBPoint pt = TSDB.GetTagByName(tagName);
            TSDBResult tsdbesult = new TSDBResult();
            if (!DateTime.TryParse(startTime.ToString(), out DateTime st))
                throw new Exception("Неверный формат даты");
            if (!DateTime.TryParse(endTime.ToString(), out DateTime et))
                throw new Exception("Неверный формат даты");
            int count = Convert.ToInt32(pt.Data.Summary(st, et, TSDBSummaryType.Count, TSDBSummaryCalculationBasis.EventWeighted).Value);
            if (count > 0)
            {
                TSDBValue result = pt.Data.RecordedValues(new DateTime(st.Ticks, DateTimeKind.Local), new DateTime(et.Ticks, DateTimeKind.Local), BoundaryTypeConstants.btInside)
                     .Where(item =>
                     {
                         if (double.TryParse(item.Value.ToString(), out double val)) return val <= value;
                         return false;
                     }).DefaultIfEmpty(new TSDBValue(pt.Name, DateTime.MinValue, "NoData", String.Empty, Quality.bad)).First();
                tsdbesult.Value = result.Value;
                tsdbesult.Digital = pt.ValueType == TSDBValueType.SET ? result.DigitalSetValue : result.Value.ToString();
                tsdbesult.Time = result.TimestampUTC.ToLocalTime();
                tsdbesult.Good = result.IsGood();
            }
            return tsdbesult;
        }
        //Первое значение меньше Value
        public static TSDBResult FindLT(string tagName, object startTime, object endTime, double value)
        {
            TSDBPoint pt = TSDB.GetTagByName(tagName);
            TSDBResult tsdbesult = new TSDBResult();
            if (!DateTime.TryParse(startTime.ToString(), out DateTime st))
                throw new Exception("Неверный формат даты");
            if (!DateTime.TryParse(endTime.ToString(), out DateTime et))
                throw new Exception("Неверный формат даты");
            int count = Convert.ToInt32(pt.Data.Summary(new DateTime(st.Ticks, DateTimeKind.Local), new DateTime(et.Ticks, DateTimeKind.Local), TSDBSummaryType.Count, TSDBSummaryCalculationBasis.EventWeighted).Value);
            if (count > 0)
            {
                TSDBValue result = pt.Data.RecordedValues(new DateTime(st.Ticks, DateTimeKind.Local), new DateTime(et.Ticks, DateTimeKind.Local), BoundaryTypeConstants.btInside)
                     .Where(item =>
                     {
                         if (double.TryParse(item.Value.ToString(), out double val)) return val < value;
                         return false;
                     }).DefaultIfEmpty(new TSDBValue(pt.Name, DateTime.MinValue, "NoData", String.Empty, Quality.bad)).First();
                tsdbesult.Value = result.Value;
                tsdbesult.Digital = pt.ValueType == TSDBValueType.SET ? result.DigitalSetValue : result.Value.ToString();
                tsdbesult.Time = result.TimestampUTC.ToLocalTime();
                tsdbesult.Good = result.IsGood();
            }
            return tsdbesult;
        }*/
    }
}
