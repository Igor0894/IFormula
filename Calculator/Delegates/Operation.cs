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
}
