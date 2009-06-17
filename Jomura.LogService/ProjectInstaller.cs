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
            //�C���X�g�[�����㎩���J�n
            System.ServiceProcess.ServiceController sc =
                new System.ServiceProcess.ServiceController();
            sc.ServiceName = "Jomura.LogService";
            sc.Start();
        }

        private void ProjectInstaller_BeforeUninstall(object sender, InstallEventArgs e)
        {
            //�A���C���X�g�[�����O������~
            System.ServiceProcess.ServiceController sc =
                new System.ServiceProcess.ServiceController();
            sc.ServiceName = "Jomura.LogService";
            sc.Stop();
        }
    }
}