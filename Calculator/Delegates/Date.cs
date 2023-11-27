namespace Interpreter.Delegates
{
    public static class Date
    {
        public static DateTime Bod(object dateTime, double offset = 0)
        {
            if (!DateTime.TryParse(dateTime.ToString(), out DateTime timeStamp))
                throw new Exception("Неверный формат даты");
            //return offset == 0 ? timeStamp.Date : timeStamp.AddMinutes(-(offset + 1)).Date.AddMinutes(offset);
            return offset == 0 ? timeStamp.Date : timeStamp.Date.AddMinutes(offset);
        }
        public static DateTime Bom(object dateTime, double offset = 0)
        {
            if (!DateTime.TryParse(dateTime.ToString(), out DateTime timeStamp))
                throw new Exception("Неверный формат даты");
            //if (offset != 0) timeStamp = timeStamp.AddMinutes(-(offset + 1));
            return new DateTime(timeStamp.Year, timeStamp.Month, 1).AddMinutes(offset);
        }
        public static DateTime Boy(object dateTime, double offset = 0)
        {
            if (!DateTime.TryParse(dateTime.ToString(), out DateTime timeStamp))
                throw new Exception("Неверный формат даты");
            //if (offset != 0) timeStamp = timeStamp.AddMinutes(-(offset + 1));
            return new DateTime(timeStamp.Year, 1, 1).AddMinutes(offset);
        }
        public static int DaysInMonth(int year, int month)
        {
            return DateTime.DaysInMonth(year, month);
        }
        public static DateTime AddDays(object dateTime, double count)
        {
            if (!DateTime.TryParse(dateTime.ToString(), out DateTime timeStamp))
                throw new Exception("Неверный формат даты");
            return timeStamp.AddDays(count);
        }
        public static DateTime AddHours(object dateTime, double count)
        {
            if (!DateTime.TryParse(dateTime.ToString(), out DateTime timeStamp))
                throw new Exception("Неверный формат даты");
            return timeStamp.AddHours(count);
        }
        public static DateTime AddMinutes(object dateTime, double count)
        {
            if (!DateTime.TryParse(dateTime.ToString(), out DateTime timeStamp))
                throw new Exception("Неверный формат даты");
            return timeStamp.AddMinutes(count);
        }
        public static DateTime AddSeconds(object dateTime, double count)
        {
            if (!DateTime.TryParse(dateTime.ToString(), out DateTime timeStamp))
                throw new Exception("Неверный формат даты");
            return timeStamp.AddSeconds(count);
        }
        public static DateTime AddMonths(object dateTime, int count)
        {
            if (!DateTime.TryParse(dateTime.ToString(), out DateTime timeStamp))
                throw new Exception("Неверный формат даты");

            return timeStamp.AddMonths(count);
        }
        public static DateTime AddYears(object dateTime, int count)
        {
            if (!DateTime.TryParse(dateTime.ToString(), out DateTime timeStamp))
                throw new Exception("Неверный формат даты");
            return timeStamp.AddYears(count);
        }
        public static int Year(object dateTime)
        {
            if (!DateTime.TryParse(dateTime.ToString(), out DateTime timeStamp))
                throw new Exception("Неверный формат даты");
            return timeStamp.Year;
        }
        public static int Month(object dateTime)
        {
            if (!DateTime.TryParse(dateTime.ToString(), out DateTime timeStamp))
                throw new Exception("Неверный формат даты");
            return timeStamp.Month;
        }
        public static int Day(object dateTime)
        {
            if (!DateTime.TryParse(dateTime.ToString(), out DateTime timeStamp))
                throw new Exception("Неверный формат даты");
            return timeStamp.Day;
        }
        public static int Hour(object dateTime)
        {
            if (!DateTime.TryParse(dateTime.ToString(), out DateTime timeStamp))
                throw new Exception("Неверный формат даты");
            return timeStamp.Hour;
        }
        public static int Minute(object dateTime)
        {
            if (!DateTime.TryParse(dateTime.ToString(), out DateTime timeStamp))
                throw new Exception("Неверный формат даты");
            return timeStamp.Minute;
        }
        public static int Second(object dateTime)
        {
            if (!DateTime.TryParse(dateTime.ToString(), out DateTime timeStamp))
                throw new Exception("Неверный формат даты");
            return timeStamp.Second;
        }
        public static double DaySec(object dateTime)
        {
            if (!DateTime.TryParse(dateTime.ToString(), out DateTime timeStamp))
                throw new Exception("Неверный формат даты");
            return (timeStamp - timeStamp.Date).TotalSeconds;
        }
        public static DateTime operator +(DateTime baseTime, string addingTime)
        {
            return double.Parse(result1.Value.ToString()) + double.Parse(result2.Value.ToString());
        }
    }
}
