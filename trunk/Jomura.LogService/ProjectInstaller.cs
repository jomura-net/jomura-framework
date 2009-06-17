using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;

namespace Jomura.LogService
{
    [RunInstaller(true)]
    public partial class ProjectInstaller : Installer
    {
        public ProjectInstaller()
        {
            InitializeComponent();
        }

        private void ProjectInstaller_Committed(object sender, InstallEventArgs e)
        {
            //インストール直後自動開始
            System.ServiceProcess.ServiceController sc =
                new System.ServiceProcess.ServiceController();
            sc.ServiceName = "Jomura.LogService";
            sc.Start();
        }

        private void ProjectInstaller_BeforeUninstall(object sender, InstallEventArgs e)
        {
            //アンインストール直前自動停止
            System.ServiceProcess.ServiceController sc =
                new System.ServiceProcess.ServiceController();
            sc.ServiceName = "Jomura.LogService";
            sc.Stop();
        }
    }
}