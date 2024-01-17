namespace Interpreter.Delegates
{
    public static class Operation
    {
        public static object IF(bool condition, object trueCondition, object falseCondition)
        {
            if (condition)
                return trueCondition;
            return falseCondition;
        }
        }
        public static int Floor(object input)
        {
            return Convert.ToInt32(Math.Round(decimal.Parse(input.ToString())));
        }
        public static bool Compare(object str1, object str2)
        {
            if (string.Compare(str1.ToString().ToLower(), str2.ToString().ToLower()) == 0) return true;
            else return false;
        }
        public static string Left(object input, int len) 
        {
            string output = input.ToString().Substring(0, len);
            return output;
        }
    }
}
