using System;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using log4net.Appender;
using log4net.Core;

namespace MongoAppender
{
    public class MongoDBAppender : AppenderSkeleton
    {
        RingBuffer<LoggingEvent> pendingAppends;
        readonly ManualResetEvent manualResetEvent;
        bool shuttingDown;
        bool hasFinished;
        bool forceStop;
        bool logBufferOverflow;
        int bufferOverflowCounter;
        DateTime lastLoggedBufferOverflow;
        const int queueSizeLimit = 1000;

        public string ConnectionString { get; set; }

        public string ConnectionStringName { get; set; }

        public string CollectionName { get; set; }

        public MongoDBAppender()
        {
            manualResetEvent = new ManualResetEvent(false);
        }

        public override void ActivateOptions()
        {
            base.ActivateOptions();
            pendingAppends = new RingBuffer<LoggingEvent>(queueSizeLimit);
            pendingAppends.BufferOverflow += OnBufferOverflow;
            StartAppendTask();
        }

        protected override void Append(LoggingEvent[] loggingEvents)
        {
            Array.ForEach(loggingEvents, Append);
        }

        protected override void Append(LoggingEvent loggingEvent)
        {
            if (FilterEvent(loggingEvent))
            {
                pendingAppends.Enqueue(loggingEvent);
            }
        }

        protected override void OnClose()
        {
            shuttingDown = true;
            manualResetEvent.WaitOne(TimeSpan.FromSeconds(5));

            if (!hasFinished)
            {
                forceStop = true;
                InsertLog(BsonExtension.BuildBufferClearError());
            }
            base.OnClose();
        }

        void StartAppendTask()
        {
            if (!shuttingDown)
            {
                var appendTask = new Task(AppendLoggingEvents, TaskCreationOptions.LongRunning);
                appendTask.LogErrors(LogAppenderError).ContinueWith(x => StartAppendTask()).LogErrors(LogAppenderError);
                appendTask.Start();
            }
        }

        void LogAppenderError(string logMessage, Exception exception)
        {
            InsertLog(BsonExtension.BuildLogAppenderError(logMessage, exception));
        }

        void AppendLoggingEvents()
        {
            LoggingEvent loggingEventToAppend;
            while (!shuttingDown)
            {
                if (logBufferOverflow)
                {
                    LogBufferOverflowError();
                    logBufferOverflow = false;
                    bufferOverflowCounter = 0;
                    lastLoggedBufferOverflow = DateTime.UtcNow;
                }

                while (!pendingAppends.TryDequeue(out loggingEventToAppend))
                {
                    Thread.Sleep(10);
                    if (shuttingDown)
                    {
                        break;
                    }
                }
                if (loggingEventToAppend == null)
                {
                    continue;
                }
                InsertLog(BsonExtension.BuildDocument(loggingEventToAppend));
            }

            while (pendingAppends.TryDequeue(out loggingEventToAppend) && !forceStop)
            {
                InsertLog(BsonExtension.BuildDocument(loggingEventToAppend));
            }
            hasFinished = true;
            manualResetEvent.Set();
        }

        MongoCollection GetCollection()
        {
            return GetDatabase().GetCollection(CollectionName);
        }

        MongoDatabase GetDatabase()
        {          
            return new MongoClient(ConnectionString).GetServer().GetDatabase(new MongoUrl(ConnectionString).DatabaseName);
        }

        void InsertLog(BsonDocument doc)
        {
            try
            {
                var collection = GetCollection();
                collection.Insert(doc);
            }
            catch (Exception)
            {

            }
        }

        void LogBufferOverflowError()
        {
            InsertLog(BsonExtension.BuildBufferOverflowError(bufferOverflowCounter, queueSizeLimit));
        }

        void OnBufferOverflow(object sender, EventArgs eventArgs)
        {
            bufferOverflowCounter++;
            if (!logBufferOverflow)
            {
                logBufferOverflow |= lastLoggedBufferOverflow < DateTime.UtcNow.AddSeconds(-30);
            }
        }
    }
}
