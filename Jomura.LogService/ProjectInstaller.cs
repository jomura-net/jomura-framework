using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;

namespace Jomura.log4netRemotingService
{
    [RunInstaller(true)]
    public partial class ProjectInstaller : Installer
    {
        /// <summary>
        /// auto-genarated Constructor
        /// </summary>
        public ProjectInstaller()
        {
            InitializeComponent();
        }

        private void ProjectInstaller_Committed(object sender, InstallEventArgs e)
        {
            //�C���X�g�[�����㎩���J�n
            System.ServiceProcess.ServiceController sc =
                new System.ServiceProcess.ServiceController();
            sc.ServiceName = "log4netRemotingService";
            sc.Start();
        }

        private void ProjectInstaller_BeforeUninstall(object sender, InstallEventArgs e)
        {
            //�A���C���X�g�[�����O������~
            System.ServiceProcess.ServiceController sc =
                new System.ServiceProcess.ServiceController();
            sc.ServiceName = "log4netRemotingService";
            if (sc.CanStop)
            {
                sc.Stop();
            }
        }
    }
}