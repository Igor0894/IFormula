using ApplicationServices.Services;
using System.Net;

namespace IFormula.Middlewares
{
    public class ExceptionsMiddleware : IMiddleware
    {
        private ILogger<CalcService> Logger { get; set; }
        public ExceptionsMiddleware(ILogger<CalcService> logger)
        {
            Logger = logger;
        }
        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            try
            {
                await next(context);
            }
            catch (Exception e)
            {
                Logger.LogError($"Ошибка при выполнении запроса: {e.Message} \r {e.StackTrace}");
                var message = e.Message;
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                context.Response.ContentType = "text/plain";
                await context.Response.WriteAsync(message);
            }
        }
    }
}
