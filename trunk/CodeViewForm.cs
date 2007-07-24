using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;

using Rtf;

namespace Cat
{
    public partial class CodeViewForm : Form
    {
        static CodeViewForm form = new CodeViewForm();
        CatRtfWriter mWriter = new CatRtfWriter();
        Session mpSession;
        string msOptions;
        ToolTip mToolTip = new ToolTip();

        public CodeViewForm()
        {
            InitializeComponent();
        }

        private void CodeViewForm_Load(object sender, EventArgs e)
        {
            // Create the ToolTip and associate with the Form container.
            ToolTip toolTip1 = new ToolTip();

            // Set up the delays for the ToolTip.
            toolTip1.AutoPopDelay = 5000;
            toolTip1.InitialDelay = 500;
            toolTip1.ReshowDelay = 500;
            
            // Force the ToolTip text to be displayed whether or not the form is active.
            toolTip1.ShowAlways = true;

            // Set up the ToolTip text for the Button and Checkbox.
            toolTip1.SetToolTip(this.mEdit, "some text");
        }

        public void Display(string sOptions, Session pSession)
        {
            mpSession = pSession;
            msOptions = sOptions;
            ShowDialog();
        }

        public static void Show(string sOptions)
        {
            form.Display(sOptions, Session.GetGlobalSession());
        }

        private void mEdit_TextChanged(object sender, EventArgs e)
        {

        }

        private void CodeViewForm_Shown(object sender, EventArgs e)
        {
            Rebuild();
        }

        private void mEdit_MouseMove(object sender, MouseEventArgs e)
        {
            DefTarget def = mWriter.GetDefTargetFromCallPos(MouseEventToPos(e));
            
            if (def != null)
            {
                DefinedFunction f = def.Data;
                mToolTip.SetToolTip(mEdit, f.GetInfoString());
            }
            else
            {
                mToolTip.RemoveAll();
            }
        }

        private int MouseEventToPos(MouseEventArgs e)
        {
            return mEdit.GetCharIndexFromPosition(new Point(e.X, e.Y));
        }

        private void mEdit_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            DefTarget def = mWriter.GetDefTargetFromCallPos(MouseEventToPos(e));
            if (def == null)
                return;
            mEdit.SelectionLength = 0;
            mEdit.SelectionStart = def.Begin;
            mEdit.ScrollToCaret();
        }

        private void Rebuild()
        {
            int nPos = mEdit.GetCharIndexFromPosition(new Point(0, 0));
            double dPos = 0.0;
            if (mEdit.TextLength > 0)
                dPos = (double)nPos / (double)mEdit.TextLength;            

            mWriter.Clear();
            mWriter.ShowComments(ShowCommentsMenuItem.Checked);
            mWriter.ShowImplementation(ShowImplMenuItem.Checked);
            mWriter.ShowInferredTypes(ShowInferredTypeMenuItem.Checked);
            mWriter.ShowCondensed(ShowCondensedMenuItem.Checked);
            mpSession.Output(mWriter);
            mEdit.Rtf = mWriter.GetRtf(mEdit.Font);

            dPos = (double)mEdit.TextLength * dPos;
            nPos = (int)dPos;
            mEdit.SelectionStart = nPos;
            mEdit.ScrollToCaret();
        }

        private void ShowCommentsMenuItem_Click(object sender, EventArgs e)
        {
            Rebuild();
        }

        private void ShowImplMenuItem_Click(object sender, EventArgs e)
        {
            Rebuild();
        }

        private void ShowInferredTypeMenuItem_Click(object sender, EventArgs e)
        {
            Rebuild();
        }

        private void ShowCondensedMenuItem_Click(object sender, EventArgs e)
        {
            Rebuild();
        }

        private void mEdit_MouseClick(object sender, MouseEventArgs e)
        {
        }

        private void contextMenuStrip1_Closed(object sender, ToolStripDropDownClosedEventArgs e)
        {
            EditDefMenuItem.Enabled = false;
            EditDefMenuItem.Text = "Edit Definition";
        }

        private void toolStripMenuItem2_Click(object sender, EventArgs e)
        {

        }

        private void EditDefMenuItem_Click(object sender, EventArgs e)
        {
            DefinedFunction f = EditDefMenuItem.Tag as DefinedFunction;
            DefinedFunction g = EditDefForm.EditFunction(f);
            if (g != null)
            {
                Session.GetGlobalSession().RedefineFunction(g);
            }
        }

        private void mEdit_VScroll(object sender, EventArgs e)
        {
        }

        private void mEdit_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                ContextMenu m = mEdit.ContextMenu;
                int nPos = MouseEventToPos(e);
                DefinedFunction def = mWriter.GetDefFromPos(nPos);

                if (def != null)
                {
                    EditDefMenuItem.Enabled = true;
                    string s = "Edit Definition of " + def.GetName();
                    EditDefMenuItem.Text = s;
                    EditDefMenuItem.Tag = def;
                }
            }
        }
   }

}