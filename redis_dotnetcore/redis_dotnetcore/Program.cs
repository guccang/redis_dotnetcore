using System;
using System.Collections.Generic;
using StackExchange.Redis;
using log4net;
using System.IO;

namespace redis_dotnetcore
{
    class LogConfig
    {
        private static string _logRepositoryName = "RedisLog";
        public static ILog GetLog(Type type)
        {
            return LogManager.GetLogger(_logRepositoryName, type);
        }
        public static void InitLog()
        {
            GlobalContext.Properties["FolderName"] = AppDomain.CurrentDomain.BaseDirectory;
            GlobalContext.Properties["FileName"] = "GBT.log";
            //string fileName = AppDomain.CurrentDomain.BaseDirectory + "config.log4net";
            string fileName = Environment.CurrentDirectory + "/config.log4net";
            var rep = log4net.LogManager.CreateRepository(_logRepositoryName);
            var ret = log4net.Config.XmlConfigurator.ConfigureAndWatch(rep, new FileInfo(fileName));
        }
    }

    class Program
    {
        class SubscriberData
        {
            public string ret;
        }
        private static Queue<SubscriberData> _msgQueue; 
        static void Main(string[] args)
        {
            int counter = 0;
            LogConfig.InitLog();
            var log = LogConfig.GetLog(typeof(Program));
            _msgQueue = new Queue<SubscriberData>();
            ConnectionMultiplexer redis = ConnectionMultiplexer.Connect("localhost");
            redis.ConnectionFailed += (_, e) => 
            {
                log.Error("connection failed");
            } ;
            redis.ConnectionRestored += (_, e) => 
            {
                log.Info("connection Restored");
            };
            ISubscriber sub = redis.GetSubscriber();
            sub.Subscribe("foo", (channel, message) =>
            {
                var s = new SubscriberData() { ret = (string)message };
                lock(_msgQueue)
                    _msgQueue.Enqueue(s);
            });

            log.Debug("read message from redis");
            DateTime begin = DateTime.Now;
            while(true)
            {
                for(int i=0; i< 20000;++i)
                {
                    if(sub.IsConnected())
                        sub.Publish("foo", $"{counter++}  where are you!!! {(DateTime.Now - begin).TotalSeconds}");
                }
                //System.Threading.Thread.Sleep(1);
                lock(_msgQueue)
                {
                    int count = _msgQueue.Count;
                    while(count>0)
                    {
                        count--;
                        var s = _msgQueue.Dequeue() as SubscriberData;
                        if (null != s)
                            log.Debug($"{s.ret}");
                    }
                }
            }
        }
    }
}
