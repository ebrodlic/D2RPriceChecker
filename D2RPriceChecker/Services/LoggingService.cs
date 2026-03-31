using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace D2RPriceChecker.Services
{
    public static class LoggingService
    {
        public static void Initialize(string appDir)
        {
            // Define log directory
            var logDir = Path.Combine(appDir, "Logs");

            // Ensure directory exists
            Directory.CreateDirectory(logDir);

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
              //.WriteTo.Console() // optional
                .WriteTo.File(
                    Path.Combine(logDir, "log-.txt"),
                    rollingInterval: RollingInterval.Day, // creates new file each day
                    retainedFileCountLimit: 30,           // keep last 30 files
                    rollOnFileSizeLimit: true)
                .CreateLogger();

            Log.Information("Logging initialized in {LogDir}", logDir);
        }

        public static void Info(string message)
        {
            Log.Information(message);
        }

        public static void Error(string message, Exception ex)
        {
            Log.Error(ex, message);
        }
    }
}
