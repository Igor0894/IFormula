namespace ApplicationServices.Scheduller.Models
{
    [Serializable]
    public class CalcNode
#nullable disable
    {
        public string SearchAttribute { get; set; }
        public string SearchModel { get; set; }
        public string SearchTemplate { get; set; }
        public string cronExpression { get; set; }
    }
    public enum CalcMode
    {
        Schedulled,
        Recalc,
        Trigger
    }
}
