using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Remoting.Channels.Ipc;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting;
using System.Diagnostics;
using log4net.Repository.Hierarchy;
using log4net;
using System.Runtime.Remoting.Channels.Tcp;
using log4net.Core;

namespace LogClient
{
    class Program
    {
        static readonly ILog log = LogManager.GetLogger(
            System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        static void Main(string[] args)
        {
            for (int i = 0, max = 10; i < max; i++)
            {
                log.Debug("Debug");
                log.Info("info");
                log.Debug("Debug");
                log.Error("Error", new ApplicationException("アプリエラー"));
            }

            try
            {
                int.Parse("a");
            }
            catch (Exception ex)
            {
                log.Error("例外が発生しました。", ex);
            }

            Debug.WriteLine("Events");

            /*
            TcpClientChannel clientChannel = new TcpClientChannel();
            ChannelServices.RegisterChannel(clientChannel, false);

            // Marshal the sink object
            RemotingServices.Marshal(RemoteLoggingSinkImpl.Instance,
                "LoggingSink", typeof(log4net.Appender.RemotingAppender.IRemoteLoggingSink));

            RemoteLoggingSinkImpl impl = RemoteLoggingSinkImpl.Instance;
            Debug.WriteLine("Events : " + impl.Events.Length);

            LoggingEvent[] events = new LoggingEvent[2];
            events[0] = new LoggingEvent(new LoggingEventData());

            for (int i = 0, max = 10; i < max; i++)
            {
                impl.LogEvents(events);
                Debug.WriteLine("Events : " + impl.Events.Length);
            }
             */
        }
    }
}
