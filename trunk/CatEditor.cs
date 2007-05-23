using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;

namespace Cat
{
    public partial class CatEditor : Form
    {
        public CatEditor()
        {
            InitializeComponent();
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void richTextBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
        }


        private void richTextBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control)
            {
                switch (e.KeyCode)
                {
                    case (Keys.D) :
                        AddText("define { ", " }");
                        e.Handled = true;
                        return;
                    case (Keys.M) :
                        AddText("macro { ", "} => { }");
                        e.Handled = true;
                        return;
                }
            }
        }

        private void AddText(string s1, string s2)
        {
            int nPos = edit.SelectionStart;
            int nLine = edit.GetLineFromCharIndex(nPos);
            int nLinePos = edit.GetFirstCharIndexFromLine(nLine);
            Trace.Assert(nPos >= nLinePos);
            int nCharOffset = nPos - nLinePos;
            string sLine = edit.Lines[nLinePos];
            int nFirstHalf = 
        }
    }
}