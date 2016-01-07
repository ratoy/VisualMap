﻿using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Diagnostics;
using System.Globalization;
using System.CodeDom.Compiler;
using System.Windows.Forms;
using System.ComponentModel;
using System.Xml;

using ICSharpCode.Core;
using SharpAppCore.Pad;
using SharpAppCore.ViewContent;
using SharpAppCore.Workbench;
using WinFormGui.Common;

namespace WinFormGui.WorkBench
{
    /// <summary>
    /// This is the a Workspace with a multiple document interface.
    /// </summary>
    public class DefaultWorkbench : Form, IWorkbench
    {
        // 引自 SharpDevelop Source Base/Commands/MenuItemBuilders.cs
        class MyMenuItem : MenuCommand
        {
            PadDescriptor padDescriptor;

            public MyMenuItem(PadDescriptor padDescriptor)
                : base(null, null)
            {
                this.padDescriptor = padDescriptor;
                Text = StringParser.Parse(padDescriptor.Title);

                if (padDescriptor.Icon != null)
                {
                    base.Image = IconService.GetBitmap(padDescriptor.Icon);
                }

                if (padDescriptor.Shortcut != null)
                {
                    ShortcutKeys = ICSharpCode.Core.MenuCommand.ParseShortcut(padDescriptor.Shortcut);
                }
            }

            protected override void OnClick(EventArgs e)
            {
                base.OnClick(e);
                padDescriptor.BringPadToFront();
            }

        }

        #region 变量声明

        readonly static string mainMenuPath = "/SharpDevelop/Workbench/MainMenu";
        readonly static string viewContentPath = "/SharpDevelop/Workbench/Pads";
        readonly static string toolbarPath = "/SharpDevelop/ViewContent/Toolbar";

        public MenuStrip TopMenu = null;
        IWorkbenchLayout layout = null;
        ToolStripItem[] _ViewItem;
        public ToolStrip TopToolStrip = null;
        System.Windows.Forms.Timer toolbarUpdateTimer;

        List<PadDescriptor> viewContentCollection = new List<PadDescriptor>();
        List<IViewContent> workbenchContentCollection = new List<IViewContent>();

        const string _BoundProperty = "FormBounds";
        const string _WindowIsMaximized = "WindowIsMaximized";
        const string _WindowIsFullScreen = "WindowIsFullScreen";
        Rectangle _NormalBounds;
        FormWindowState _WindowState = FormWindowState.Normal;
        bool _FullScreen = false;

        #endregion

        public DefaultWorkbench()
        {
            Text = "MapToolkit";
            Icon = ResourceService.GetIcon("Satellite");

            StartPosition = FormStartPosition.Manual;
            _NormalBounds = PropertyService.Get<Rectangle>(_BoundProperty, new Rectangle(60, 80, 640, 480));
            Bounds = _NormalBounds;
            bool bMax = PropertyService.Get<bool>(_WindowIsMaximized, false);
            if (bMax)
            {
                _WindowState = FormWindowState.Maximized;
                WindowState = FormWindowState.Maximized;
            }
            _FullScreen = PropertyService.Get<bool>(_WindowIsFullScreen, false);
            if (_FullScreen)
            {
                FormBorderStyle = FormBorderStyle.None;
                WindowState = FormWindowState.Maximized;
            }

            AllowDrop = true;

            PadDescriptor.BringPadToFrontEvent += delegate(PadDescriptor padDesc)
            {
                for (int i = 0; i < PadContentCollection.Count; i++)
                {
                    PadDescriptor pd = PadContentCollection[i];
                    if (pd.Equals(padDesc))
                    {
                        layout.ShowPad(padDesc, true);
                        return;
                    }
                    ShowPad(padDesc);
                }
            };
        }

        public void InitializeWorkspace()
        {
            UpdateRenderer();

            try
            {
                ArrayList contents = AddInTree.GetTreeNode(viewContentPath).BuildChildItems(this);
                ArrayList viewMenuItems = new ArrayList();
                foreach (PadDescriptor content in contents)
                {
                    if (content != null)
                    {
                        ShowPad(content);
                        viewMenuItems.Add(new MyMenuItem(content));
                    }
                }
                if (viewMenuItems.Count > 0)
                    _ViewItem = viewMenuItems.ToArray(typeof(ToolStripItem)) as ToolStripItem[];
            }
            catch { }

            CreateMainMenu();
            CreateToolbar();

            toolbarUpdateTimer = new System.Windows.Forms.Timer();
            toolbarUpdateTimer.Tick += delegate
            {
                UpdateMenus();
                UpdateTools();
            };
            toolbarUpdateTimer.Interval = 500;
            toolbarUpdateTimer.Start();

            RightToLeftConverter.Convert(this);
        }

        #region Override methods

        protected override void OnClosing(CancelEventArgs e)
        {
            layout.Detach();

            while (WorkbenchSingleton.Workbench.ViewContentCollection.Count > 0)
            {
                IViewContent content = WorkbenchSingleton.Workbench.ViewContentCollection[0];
                if (content.WorkbenchWindow == null)
                {
                    LoggingService.Warn("Content with empty WorkbenchWindow found");
                    WorkbenchSingleton.Workbench.ViewContentCollection.RemoveAt(0);
                }
                else
                {
                    content.WorkbenchWindow.CloseWindow(false);
                    if (WorkbenchSingleton.Workbench.ViewContentCollection.IndexOf(content) >= 0)
                    {
                        e.Cancel = true;
                        return;
                    }
                }
            }

            foreach (PadDescriptor padDescriptor in PadContentCollection)
            {
                padDescriptor.Dispose();
            }

            if (FullScreen)
            {
                PropertyService.Set<bool>(_WindowIsFullScreen, true);
                if (_WindowState == FormWindowState.Maximized)
                    PropertyService.Set<bool>(_WindowIsMaximized, true);
                else
                    PropertyService.Set<bool>(_WindowIsMaximized, false);
            }
            else
            {
                PropertyService.Set<bool>(_WindowIsFullScreen, false);
                if (WindowState == FormWindowState.Maximized)
                    PropertyService.Set<bool>(_WindowIsMaximized, true);
                else
                {
                    PropertyService.Set<bool>(_WindowIsMaximized, false);
                    PropertyService.Set<Rectangle>(_BoundProperty, Bounds);
                }
            }

            base.OnClosing(e);
        }

        protected override void OnDragEnter(DragEventArgs e)
        {
            base.OnDragEnter(e);
            if (e.Data != null && e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                foreach (string file in files)
                {
                    if (File.Exists(file))
                    {
                        e.Effect = DragDropEffects.Copy;
                        return;
                    }
                }
            }
            e.Effect = DragDropEffects.None;
        }

        protected override void OnDragDrop(DragEventArgs e)
        {
            base.OnDragDrop(e);
            if (e.Data != null && e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                foreach (string file in files)
                {
                    //if (File.Exists(file)) {
                    //    IProjectLoader loader = ProjectService.GetProjectLoader(file);
                    //    if (loader != null) {
                    //FileUtility.ObservedLoad(new NamedFileOperationDelegate(loader.Load), file);
                    //} else {
                    //FileService.OpenFile(file);
                    //}
                }
            }
        }

        #endregion

        #region Implementation of IWorkbench

        public string Title
        {
            get
            {
                return Text;
            }
            set
            {
                Text = value;
            }
        }

        public List<IViewContent> ViewContentCollection
        {
            get
            {
                System.Diagnostics.Debug.Assert(workbenchContentCollection != null);
                return workbenchContentCollection;
            }
        }

        public List<PadDescriptor> PadContentCollection
        {
            get
            {
                System.Diagnostics.Debug.Assert(viewContentCollection != null);
                return viewContentCollection;
            }
        }

        public IWorkbenchWindow ActiveWorkbenchWindow
        {
            get
            {
                if (layout == null)
                {
                    return null;
                }
                return layout.ActiveWorkbenchwindow;
            }
        }

        public object ActiveContent
        {
            get
            {
                if (layout == null)
                {
                    return null;
                }
                return layout.ActiveContent;
            }
        }

        public IWorkbenchLayout WorkbenchLayout
        {
            get
            {
                return layout;
            }
            set
            {
                if (layout != null)
                {
                    layout.Detach();
                }
                layout = value;
                value.Attach(this);
            }
        }

        public virtual void ShowView(IViewContent content)
        {
            System.Diagnostics.Debug.Assert(layout != null);

            ViewContentCollection.Add(content);
            layout.ShowView(content);
            content.WorkbenchWindow.SelectWindow();
        }

        public virtual void CloseView(IViewContent content)
        {
            if (ViewContentCollection.Contains(content))
            {
                ViewContentCollection.Remove(content);
            }
            content.Dispose();
            content = null;
        }

        public virtual void ShowPad(PadDescriptor content)
        {
            PadContentCollection.Add(content);

            if (layout != null)
            {
                layout.ShowPad(content);
            }
        }

        public PadDescriptor GetPad(Type type)
        {
            foreach (PadDescriptor pad in PadContentCollection)
            {
                if (pad.Class == type.FullName)
                {
                    return pad;
                }
            }
            return null;
        }

        public void CloseAllViews()
        {
            try
            {
                List<IViewContent> fullList = new List<IViewContent>(workbenchContentCollection);
                foreach (IViewContent content in fullList)
                {
                    IWorkbenchWindow window = content.WorkbenchWindow;
                    window.CloseWindow(false);
                }
                workbenchContentCollection.Clear();
            }
            finally
            {
            }
        }

        public void RedrawAllComponents()
        {
            RightToLeftConverter.ConvertRecursive(this);

            foreach (ToolStripItem item in TopMenu.Items)
            {
                if (item is IStatusUpdate)
                    ((IStatusUpdate)item).UpdateText();
            }

            foreach (IViewContent content in workbenchContentCollection)
            {
                if (content.WorkbenchWindow != null)
                {
                    content.WorkbenchWindow.RedrawContent();
                }
            }

            foreach (PadDescriptor content in viewContentCollection)
            {
                content.RedrawContent();
            }

            if (layout != null)
            {
                layout.RedrawAllComponents();
            }
        }

        public bool FullScreen
        {
            get
            {
                return _FullScreen;
            }
            set
            {
                if (_FullScreen == value)
                    return;
                _FullScreen = value;
                if (_FullScreen)
                {
                    _WindowState = WindowState;
                    _NormalBounds = Bounds;
                    //
                    Visible = false;
                    FormBorderStyle = FormBorderStyle.None;
                    WindowState = FormWindowState.Maximized;
                    Visible = true;
                }
                else
                {
                    FormBorderStyle = FormBorderStyle.Sizable;
                    Bounds = _NormalBounds;
                    WindowState = _WindowState;
                }
            }
        }

        #endregion

        #region helper methods
        /// <summary>
        /// 自定义的工具栏
        /// </summary>
        void CreateToolbar()
        {
            TopToolStrip = ToolbarService.CreateToolStrip(this, toolbarPath);
            TopToolStrip.GripStyle = ToolStripGripStyle.Hidden;
        }

        void CreateMainMenu()
        {
            TopMenu = new MenuStrip();
            TopMenu.Items.Clear();
            try
            {
                ToolStripItem[] items = (ToolStripItem[])(AddInTree.GetTreeNode(mainMenuPath).BuildChildItems(this)).ToArray(typeof(ToolStripItem));
                TopMenu.Items.AddRange(items);
                if (_ViewItem != null)
                {
                    ToolStripMenuItem pad = new ToolStripMenuItem("&Pad");
                    pad.DropDownItems.AddRange(_ViewItem);
                    TopMenu.Items.Add(pad);
                }
                UpdateMenus();
            }
            catch (TreePathNotFoundException) { }
        }

        void UpdateMenus()
        {
            // update menu
            foreach (object o in TopMenu.Items)
            {
                if (o is IStatusUpdate)
                {
                    ((IStatusUpdate)o).UpdateStatus();
                }
            }
        }

        void UpdateTools()
        {
            foreach (object o in TopToolStrip.Items)
            {
                if (o is IStatusUpdate)
                {
                    (o as IStatusUpdate).UpdateStatus();
                }
            }

        }

        public void UpdateRenderer()
        {
            bool pro = PropertyService.Get("ICSharpCode.SharpDevelop.Gui.UseProfessionalRenderer", true);
            if (pro)
            {
                ToolStripManager.Renderer = new ToolStripProfessionalRenderer();
            }
            else
            {
                ProfessionalColorTable colorTable = new ProfessionalColorTable();
                colorTable.UseSystemColors = true;
                ToolStripManager.Renderer = new ToolStripProfessionalRenderer(colorTable);
            }
        }

        #endregion

    }
}