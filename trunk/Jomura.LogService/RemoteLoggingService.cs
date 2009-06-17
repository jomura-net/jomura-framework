using System;
using System.Configuration;
using System.IO;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.ServiceProcess;
using System.Threading;

using log4net;
using log4net.Config;
using log4net.Plugin;
using log4net.Repository;

namespace LogService
{
    /// <summary>
    /// log4netのRemotingAppenderによるログ送信を受信するWindowsサービス。<br />
    /// log4netの
    /// <a href="http://log4net.sourceforge.net/release/1.2.0.30316/doc/manual/introduction.html#plugins">
    /// RemoteLoggingServerPlugin</a>を利用しており、
    /// 本Windowsサービス自体もlog4net.dllを利用してログを出力する。
    /// </summary>
    partial class RemoteLoggingService : ServiceBase
    {
        /// <summary>
        /// コンストラクタ<br />
        /// </summary>
        public RemoteLoggingService()
        {
            InitializeComponent();
        }

        IChannel m_remotingChannel;
        ILoggerRepository m_repos;

        /// <summary>
        /// Windowsサービス開始時のイベントハンドラ。<br />
        /// <br />
        /// (1) App.configのAppSettingsから設定値を取得する。<br />
        /// 　　&quot;Jomura.Log.LogService.port&quot;
        /// : 受付TCPポート番号(無指定の場合は8085)<br />
        /// 　　&quot;Jomura.Log.LogService.sinkUri&quot;
        /// : 受付Uri(無指定の場合は"LoggingSink")<br />
        /// </summary>
        /// <param name="args">Windowsサービス引数</param>
        protected override void OnStart(string[] args)
        {
            // initial values
            int port = 8085;
            string portStr = ConfigurationManager.AppSettings
                ["Jomura.Log.LogService.port"];
            int.TryParse(portStr, out port);
            string sinkUri = ConfigurationManager.AppSettings
                ["Jomura.Log.LogService.sinkUri"] ?? "LoggingSink";
            string configPath = ConfigurationManager.AppSettings
                ["Jomura.Log.LogService.configPath"];

            m_remotingChannel = new TcpChannel(port);

            // Setup remoting server
            ChannelServices.RegisterChannel(m_remotingChannel, false);

            m_repos = LogManager.GetRepository();
            m_repos.PluginMap.Add(new RemoteLoggingServerPlugin(sinkUri));

            if (!string.IsNullOrEmpty(configPath))
            {
                XmlConfigurator.Configure(m_repos, new FileInfo(configPath));
            }
        }

        protected override void OnStop()
        {
            try
            {
                m_repos.Shutdown();
                ChannelServices.UnregisterChannel(m_remotingChannel);
            }
            catch (Exception)
            {
                //Do Nothing
            }
        }
    }
}
