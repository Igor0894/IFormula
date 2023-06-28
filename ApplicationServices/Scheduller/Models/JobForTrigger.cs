using Quartz;

namespace ApplicationServices.Scheduller.Models
{
    public class JobForTrigger<T> where T : IJob
    {
        public IJobDetail JobDetail { get; set; }
        public ITrigger Trigger { get; set; }
        public JobForTrigger(CalcNode node, string jobGroup)
        {
            JobDetail = JobBuilder.Create<T>()
                .WithIdentity(node.SearchAttribute + " trigger")
                .UsingJobData("name", node.SearchAttribute)
                .Build();
            
            Trigger = TriggerBuilder.Create()
                .WithIdentity(node.SearchAttribute, jobGroup)
                .WithSimpleSchedule(builder => builder.WithIntervalInSeconds(5).RepeatForever())
                //.WithCronSchedule(node.cronExpression)
                .Build();
        }
    }
}
