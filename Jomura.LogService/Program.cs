using System.Collections.Generic;
using System.ServiceProcess;
using System.Text;

namespace LogService
{
    static class Program
    {
        /// <summary>
        /// アプリケーションのメイン エントリ ポイントです。
        /// </summary>
        static void Main()
        {
            ServiceBase[] ServicesToRun;

            // 複数のユーザー サービスが同じプロセスで実行されている可能性があります。
            // このプロセスにもう 1 つサービスを追加するには、次の行を変更して 2 番目の
            // サービス オブジェクトを作成してください。たとえば、以下のとおりです。
            //
            //   ServicesToRun = new ServiceBase[] {new Service1(), new MySecondUserService()};
            //
            ServicesToRun = new ServiceBase[] { new RemoteLoggingService() };

            ServiceBase.Run(ServicesToRun);
        }
    }
}