using System;
using MongoDB.Bson;
using log4net.Core;
using log4net.Util;
using System.Collections;
using System.Security.Principal;
using System.Threading;

namespace MongoAppender
{
    public static class BsonExtension
    {
        public  static BsonDocument BuildDocument(LoggingEvent loggingEvent)
        {
            var doc = new BsonDocument
            {
                { "timestamp", loggingEvent.TimeStamp }, 
                { "level", loggingEvent.Level.ToString() }, 
                { "thread", loggingEvent.ThreadName }, 
                { "userName", loggingEvent.UserName }, 
                { "message", loggingEvent.RenderedMessage }, 
                { "loggerName", loggingEvent.LoggerName }, 
                { "domain", loggingEvent.Domain }, 
                { "machineName", Environment.MachineName }
            };

            if (loggingEvent.LocationInformation != null)
            {
                doc.Add("fileName", loggingEvent.LocationInformation.FileName);
                doc.Add("method", loggingEvent.LocationInformation.MethodName);
                doc.Add("lineNumber", loggingEvent.LocationInformation.LineNumber);
                doc.Add("className", loggingEvent.LocationInformation.ClassName);
            }

            if (loggingEvent.ExceptionObject != null)
            {
                doc.Add("exception", BsonExtension.BuildDocumentException(loggingEvent.ExceptionObject));
            }

            PropertiesDictionary compositeProperties = loggingEvent.GetProperties();
            if (compositeProperties != null && compositeProperties.Count > 0)
            {
                var properties = new BsonDocument();
                foreach (DictionaryEntry entry in compositeProperties)
                {
                    properties.Add(entry.Key.ToString(), entry.Value.ToString());
                }
                doc.Add("properties", properties);
            }
            return doc;
        }

        public  static BsonDocument BuildDocumentException(Exception ex)
        {
            var doc = new BsonDocument
            {
                { "message", ex.Message }, 
                { "source", ex.Source }, 
                { "stackTrace", ex.StackTrace }
            };

            if (ex.InnerException != null)
            {
                doc.Add("innerException", BuildDocumentException(ex.InnerException));
            }

            return doc;
        }

        public static BsonDocument BuildBufferOverflowError(int bufferOverflowCounter, int queueSizeLimit)
        {
            return  BsonExtension.BuildDocument(new LoggingEvent(new LoggingEventData
                    {
                        Level = Level.Error,
                        Message = string.Format("Buffer overflow. {0} logging events have been lost in the last 30 seconds. [QueueSizeLimit: {1}]", bufferOverflowCounter, queueSizeLimit),
                        TimeStamp = DateTime.UtcNow,
                        Identity = "",
                        ExceptionString = "",
                        UserName = "",
                        Domain = AppDomain.CurrentDomain.FriendlyName,
                        ThreadName = Thread.CurrentThread.ManagedThreadId.ToString(),
                        Properties = new PropertiesDictionary(),
                    }));
        }

        public static BsonDocument BuildBufferClearError()
        {
            return  BsonExtension.BuildDocument(new LoggingEvent(new LoggingEventData
                    {
                        Level = Level.Error,
                        Message = "Unable to clear out the MongoDBAppender buffer in the allotted time, forcing a shutdown",
                        TimeStamp = DateTime.UtcNow,
                        Identity = "",
                        ExceptionString = "",
                        UserName = "",
                        Domain = AppDomain.CurrentDomain.FriendlyName,
                        ThreadName = Thread.CurrentThread.ManagedThreadId.ToString(),
                       
                        Properties = new PropertiesDictionary(),
                    }));
        }

        public static BsonDocument BuildLogAppenderError(string logMessage, Exception exception)
        {
            return BsonExtension.BuildDocument(new LoggingEvent(new LoggingEventData
                    {
                        Level = Level.Error,
                        Message = "Appender exception: " + logMessage,
                        TimeStamp = DateTime.UtcNow,
                        Identity = "",
                        ExceptionString = exception.ToString(),
                        UserName = "",
                        Domain = AppDomain.CurrentDomain.FriendlyName,
                        ThreadName = Thread.CurrentThread.ManagedThreadId.ToString(),
                                         
                        Properties = new PropertiesDictionary(),
                    }));
        }
    }
}

