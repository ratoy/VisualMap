using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using ICSharpCode.Core;
using System.Resources;
using WinFormGui.WorkBench;
using System.Reflection;
using System.IO;

namespace VisualMap
{
    static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            LoggingService.Info("Starting App...");
            Assembly exe = typeof(Program).Assembly;
            FileUtility.ApplicationRootPath = Path.GetDirectoryName(exe.Location);

            CoreStartup c = new CoreStartup("MapToolkit");
            c.ConfigDirectory = FileUtility.Combine(FileUtility.ApplicationRootPath, "UserConfig") + Path.DirectorySeparatorChar;
            LoggingService.Info("Starting core services...");
            c.StartCoreServices();
            ResourceService.RegisterNeutralStrings(new ResourceManager("MapToolkit.Properties.SatRes", exe));
            ResourceService.RegisterNeutralImages(new ResourceManager("MapToolkit.Properties.SatRes", exe));

            AddInTree.Doozers.Add("Pad", new SharpAppCore.Pad.PadDoozer());
            AddInTree.Doozers.Add("DisplayBinding", new SharpAppCore.ViewContent.DisplayBindingDoozer());

            //初始化卫星和区域
            //SatGui.Services.SatService.SatInit();
            //SatGui.Services.AreaService.AreaInit();

            LoggingService.Debug("Looking for Addins...");
            c.AddAddInsFromDirectory(FileUtility.ApplicationRootPath);
            //c.ConfigureExternalAddIns(...);
            LoggingService.Debug("Loading AddinTre...");
            c.RunInitialization();

            LoggingService.Debug("Initializing workbench...");
            WorkbenchSingleton.InitializeWorkbench();

            //载入地图
            //SatGui.Services.MapService.MapInit();
            LoggingService.Debug("Starting workbench...");
            Form f = (Form)WorkbenchSingleton.Workbench;
            Application.Run(f);

            PropertyService.Save();
            //SatGui.Services.SatService.SaveStatus();
            //SatGui.Services.AreaService.SaveStatus();
            LoggingService.Info("Leaving app");
        }
    }
}
