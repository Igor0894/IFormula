using Quartz.Spi;
using Quartz;
using Microsoft.Extensions.DependencyInjection;

namespace ApplicationServices.Scheduller
{
    public class JobFactory : IJobFactory
#nullable disable
    {
        private readonly IServiceProvider _serviceProvider;
        public JobFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IJob NewJob(TriggerFiredBundle bundle, IScheduler scheduler)
        {
            return ActivatorUtilities.CreateInstance(_serviceProvider, bundle.JobDetail.JobType) as IJob;
        }

        public void ReturnJob(IJob job)
        {
        }
    }
}
