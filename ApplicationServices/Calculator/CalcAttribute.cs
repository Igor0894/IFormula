namespace ApplicationServices.Calculator
{
    public class CalcAttribute
#nullable disable
    {
        private string _expression = "";
        private int _order = 0;
        private DataSource _dataSource = new();
        private DataSource _dataReWriteSource = new();
        public string Name { get; set; }
        public string Variable { get; set; }

        public string Expression
        {
            get { return _expression; }
            set { _expression = value.Replace("'*'", "c"); }
        }
        public object Value { get; set; }
        public DataSource OutDataSource
        {
            get { return _dataSource; }
            set
            {
                _dataSource.Name = value.Name.ToLower();
                _dataSource.Type = value.Type;
                _dataSource.Time = value.Time.Replace("'*'", "c");
                _dataSource.Id = value.Id;
            }
        }
        public DataSource OutReWriteDataSource
        {
            get { return _dataReWriteSource; }
            set
            {
                _dataReWriteSource.Name = value.Name.ToLower();
                _dataReWriteSource.Type = value.Type;
                _dataReWriteSource.Id = value.Id;
            }
        }
        public int Order
        {
            get { return _order; }
            set { _order = value; }
        }
        public override string ToString()
        {
            return string.Format("Name: [{0}] Variable: [{1}] Expression: [{2}]", Name, Variable, Expression);
        }
    }
}
