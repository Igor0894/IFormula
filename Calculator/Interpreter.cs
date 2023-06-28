using DynamicExpresso;

namespace Interpreter
{
    public class Interpreter
    {
        private DynamicExpresso.Interpreter interpreter = new DynamicExpresso
            .Interpreter(InterpreterOptions.PrimitiveTypes | InterpreterOptions.SystemKeywords | InterpreterOptions.LateBindObject | InterpreterOptions.CaseInsensitive)
            .EnableAssignment(AssignmentOperators.None);
        private DateTime time = DateTime.Now;
        private int variablecount = 0;
        public DateTime CurrentTime
        {
            get => time;
            set
            {
                time = value;
                InstallVariables();
            }
        }
        public Interpreter()
        {
            interpreter.EnableAssignment(AssignmentOperators.None);
            InstallVariables();
            InstallFunctions();
        }
        public object Eval(string expression) => interpreter.Eval(@expression);
        private void InstallFunctions()
        {
            new Functions(ref interpreter);

        }
        private void InstallVariables()
        {
            interpreter.SetVariable("c", time, typeof(DateTime));
            interpreter.SetVariable("t", time.Date, typeof(DateTime));
            interpreter.SetVariable("y", time.Date.AddDays(-1), typeof(DateTime));
            interpreter.SetVariable("h", time.Date.AddHours(time.Hour), typeof(DateTime));
            interpreter.SetVariable("exit", double.MinValue + 1, typeof(double));
            interpreter.SetVariable("nooutput", double.MinValue, typeof(double));
        }
        public void SetVariable(string name, object value)
        {
            interpreter.SetVariable(name, value, value.GetType());
        }
        public string GetVariableName()
        {
            variablecount += 1;
            return "_var00" + variablecount;
        }
    }
}
