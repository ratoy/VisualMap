using System;
using System.IO;
using System.Windows.Forms;
using ICSharpCode.Core;
using WeifenLuo.WinFormsUI;
using SharpAppCore.Pad;

namespace WinFormGui.WorkBench
{
    class PadContentWrapper : DockContent
    {
        PadDescriptor padDescriptor;
        bool isInitialized = false;
        internal bool allowInitialize = false;

        public IPadContent PadContent
        {
            get
            {
                return padDescriptor.PadContent;
            }
        }

        public PadContentWrapper(PadDescriptor padDescriptor)
        {
            if (padDescriptor == null)
                throw new ArgumentNullException("padDescriptor");
            this.padDescriptor = padDescriptor;
            this.DockableAreas = ((((WeifenLuo.WinFormsUI.DockAreas.Float | WeifenLuo.WinFormsUI.DockAreas.DockLeft) |
                                    WeifenLuo.WinFormsUI.DockAreas.DockRight) |
                                   WeifenLuo.WinFormsUI.DockAreas.DockTop) |
                                  WeifenLuo.WinFormsUI.DockAreas.DockBottom);
            HideOnClose = true;
        }

        public void DetachContent()
        {
            Controls.Clear();
            padDescriptor = null;
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (Visible && Width > 0)
                ActivateContent();
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            if (Visible && Width > 0)
                ActivateContent();
        }

        /// <summary>
        /// Enables initializing the content. This is used to prevent initializing all view
        /// contents when the layout configuration is changed.
        /// </summary>
        public void AllowInitialize()
        {
            allowInitialize = true;
            if (Visible && Width > 0)
                ActivateContent();
        }

        void ActivateContent()
        {
            if (!allowInitialize)
                return;
            if (!isInitialized)
            {
                isInitialized = true;
                IPadContent content = padDescriptor.PadContent;
                if (content == null)
                    return;
                Control control = content.Control;
                control.Dock = DockStyle.Fill;
                Controls.Add(control);
            }
        }

        protected override string GetPersistString()
        {
            return padDescriptor.Class;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                if (padDescriptor != null)
                {
                    padDescriptor.Dispose();
                    padDescriptor = null;
                }
            }
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // PadContentWrapper
            // 
            this.ClientSize = new System.Drawing.Size(292, 273);
            this.Name = "PadContentWrapper";
            this.ResumeLayout(false);

        }

        DockState GetDefaultPosition()
        {
            DockState style;
            if ((padDescriptor.DefaultPosition & DefaultPadPositions.Top) != 0)
                style = DockState.DockTop;
            else if ((padDescriptor.DefaultPosition & DefaultPadPositions.Left) != 0)
                style = DockState.DockLeft;
            else if ((padDescriptor.DefaultPosition & DefaultPadPositions.Bottom) != 0)
                style = DockState.DockBottom;
            else
                style = DockState.DockRight;

            return style;
        }

        public void ShowInDefaultPosition()
        {
            DockState style=GetDefaultPosition();

            this.Show(this.DockPanel, style);
            if ((padDescriptor.DefaultPosition & DefaultPadPositions.Hidden) != 0)
                Hide();
        } 
    }
}