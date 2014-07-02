using System;
using System.Threading.Tasks;

namespace MongoAppender
{
    public static class LoggingTaskExtension
    {
      
        public static Task LogErrors(this Task task, Action<string, Exception> logMethod)
        {
            return task.ContinueWith(t => LogErrorsInner(t, logMethod), TaskContinuationOptions.OnlyOnFaulted);
        }

        public static Task LogErrorsWaitable(this Task task, Action<string, Exception> logMethod)
        {
            return task.ContinueWith(t => LogErrorsInner(t, logMethod));
        }

        static void LogErrorsInner(Task task, Action<string, Exception> logAction)
        {
            if (task.Exception != null)
            {
                logAction("Aggregate Exception with " + task.Exception.InnerExceptions.Count + " inner exceptions: ", task.Exception);
                foreach (var innerException in task.Exception.InnerExceptions)
                {
                    logAction("Inner exception from aggregate exception: ", innerException);
                }
            }
        }
    }
}
