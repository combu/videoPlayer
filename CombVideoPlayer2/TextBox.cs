using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using System.Windows.Controls;
using System.Windows.Forms;

namespace CombVideoPlayer2
{
    public partial class MyTextBox: System.Windows.Forms.RichTextBox
    {
        [SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.UnmanagedCode)]
        protected override void WndProc(ref Message m)
        {
            const int WM_HSCROLL = 522;
            switch (m.Msg)
            {
                case WM_HSCROLL:
                     
                    break;
            }
            base.WndProc(ref m);
        }

    }
}
