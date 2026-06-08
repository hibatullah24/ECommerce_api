using ECommerce_api_api.Controllers;

namespace ECommerce_api.Services
{
    public class LoggingService<T>
    {
        private readonly ILogger<T> _logger;
        private ILogger<UsersController> logger;

        // ✅ DI will now inject ILogger<T> automatically
        public LoggingService(ILogger<T> logger)
        {
            _logger = logger;
        }

        public LoggingService(ILogger<UsersController> logger)
        {
            this.logger = logger;
        }

        public void LogInfo(string message, params object[] args)
        {
            _logger.LogInformation(message, args);
        }

        public void LogWarning(string message, params object[] args)
        {
            _logger.LogWarning(message, args);
        }

        public void LogError(string message, params object[] args)
        {
            _logger.LogError(message, args);
        }

        public void LogError(Exception ex, string message, params object[] args)
        {
            _logger.LogError(ex, message, args);
        }
    }
}