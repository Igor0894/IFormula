using Quartz;

namespace ApplicationServices.Scheduller.Models
{
    public class JobForSchedule<T> where T : IJob
    {
        public IJobDetail JobDetail { get; set; }
        public ITrigger Trigger { get; set; }
        public JobForSchedule(CalcNode node, string jobGroup)
        {
            JobDetail = JobBuilder.Create<T>()
                .WithIdentity(node.SearchAttribute + " schedulle")
                .UsingJobData("name", node.SearchAttribute)
                .Build();
            
            Trigger = TriggerBuilder.Create()
                .WithIdentity(node.SearchAttribute, jobGroup)
                .WithCronSchedule(node.cronExpression)
                .Build();
        }
    }
}
