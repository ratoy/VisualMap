using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using SharpAppCore.ViewContent;
using WinFormGui.WorkBench;
using DotSpatial.Controls;

namespace WinFormGui.ViewContent
{
    public class MapView : UserControl
    {
        private Map map1;

        public MapView()
        {
            InitializeComponent();
        }

        public Map MapControl
        {
            get { return map1; }
        }

        private void InitializeComponent()
        {
            this.map1 = new DotSpatial.Controls.Map();
            this.SuspendLayout();
            // 
            // map1
            // 
            this.map1.AllowDrop = true;
            this.map1.BackColor = System.Drawing.Color.White;
            this.map1.CollectAfterDraw = false;
            this.map1.CollisionDetection = false;
            this.map1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.map1.ExtendBuffer = false;
            this.map1.FunctionMode = DotSpatial.Controls.FunctionMode.None;
            this.map1.IsBusy = false;
            this.map1.IsZoomedToMaxExtent = false;
            this.map1.Legend = null;
            this.map1.Location = new System.Drawing.Point(0, 0);
            this.map1.Name = "map1";
            this.map1.ProgressHandler = null;
            this.map1.ProjectionModeDefine = DotSpatial.Controls.ActionMode.Prompt;
            this.map1.ProjectionModeReproject = DotSpatial.Controls.ActionMode.Prompt;
            this.map1.RedrawLayersWhileResizing = false;
            this.map1.SelectionEnabled = true;
            this.map1.Size = new System.Drawing.Size(150, 150);
            this.map1.TabIndex = 0;
            // 
            // MapView
            // 
            this.Controls.Add(this.map1);
            this.Name = "MapView";
            this.ResumeLayout(false);

        }

    }
    public class MapDisplayBinding : IDisplayBinding
    {
        public bool CanCreateContentForFile(string fileName)
        {
            fileName = fileName.ToLower();
            return fileName.EndsWith(".shp");
        }

        public IViewContent CreateContentForFile(string fileName)
        {
            //载入地图
            MapViewPane browserPane = null;
            if (WorkbenchSingleton.Workbench.ActiveWorkbenchWindow != null)
            {
                browserPane = WorkbenchSingleton.Workbench.ActiveWorkbenchWindow.ViewContent as MapViewPane;
            }
            if (browserPane == null)
            {
                browserPane = new MapViewPane();
            }
            browserPane.Load(fileName);

            return browserPane;
        }
    }

    public class MapViewPane : AbstractViewContent
    {
        MapView m_MapViewPane;

        public MapViewPane()
        {
            m_MapViewPane = new MapView();
        }

        public override Control Control
        {
            get
            {
                return m_MapViewPane;
            }
        }

        public override void Dispose()
        {
        }
    }

}
