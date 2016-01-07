using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using SharpAppCore.Pad;

namespace WinFormGui.Pad
{
    public partial class LayerTree : UserControl, IPadContent
    {
        public LayerTree()
        {
            InitializeComponent();
        }

        public new Control Control
        {
            get { return this.legend1; }
        }

        public void RedrawContent()
        {
            throw new NotImplementedException();
        }
    }
}
