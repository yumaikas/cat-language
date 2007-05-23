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
                    case (Keys.Return) :
                        InsertLine();
                        e.Handled = true;
                        return;
                    case (Keys.Tab) :
                        Insert("    ");
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

            string sLine = "";
            if (nLine < edit.Lines.Length)
                sLine = edit.Lines[nLine];

            string sFirstHalf = sLine.Substring(0, nCharOffset);
            string sSecondHalf = sLine.Substring(nCharOffset + edit.SelectionLength);
            string sSel = edit.SelectedText;
            string sResult = sFirstHalf + s1 + sSel + s2 + sSecondHalf;
            int nLines = Math.Max(nLine + 1, edit.Lines.Length);
            string[] a = new string[nLines];
            a[nLine] = sResult;
            edit.Lines = a;
            edit.SelectionStart = nPos + s1.Length + sSel.Length;
            edit.SelectionLength = 0;
        }

        private void Insert(string s)
        {
            int nPos = edit.SelectionStart;
            int nLine = edit.GetLineFromCharIndex(nPos);
            int nLinePos = edit.GetFirstCharIndexFromLine(nLine);
            Trace.Assert(nPos >= nLinePos);
            int nCharOffset = nPos - nLinePos;

            string sLine = "";
            if (nLine < edit.Lines.Length)
                sLine = edit.Lines[nLine];

            string sFirstHalf = sLine.Substring(0, nCharOffset);
            string sSecondHalf = sLine.Substring(nCharOffset + edit.SelectionLength);
            string sSel = edit.SelectedText;
            string sResult = sFirstHalf + s + sSecondHalf;
            int nLines = Math.Max(nLine + 1, edit.Lines.Length);
            string[] a = new string[nLines];
            a[nLine] = sResult;
            edit.Lines = a;
            edit.SelectionStart = nPos + s.Length + sSel.Length;
            edit.SelectionLength = 0;
        }

        private void InsertLine()
        {
            int nPos = edit.SelectionStart;
            int nLine = edit.GetLineFromCharIndex(nPos);
            int nLinePos = edit.GetFirstCharIndexFromLine(nLine);
            Trace.Assert(nPos >= nLinePos);
            int nCharOffset = nPos - nLinePos;

            string sLine = "";
            if (nLine < edit.Lines.Length)
                sLine = edit.Lines[nLine];

            string sFirstHalf = sLine.Substring(0, nCharOffset);
            string sSecondHalf = sLine.Substring(nCharOffset + edit.SelectionLength);
            string sSel = edit.SelectedText;
            string sResult = sFirstHalf + s + sSecondHalf;
            int nLines = Math.Max(nLine + 1, edit.Lines.Length);
            string[] a = new string[nLines];
            a[nLine] = sResult;
            edit.Lines = a;
            edit.SelectionStart = nPos + s1.Length + sSel.Length;
            edit.SelectionLength = 0;
        }

        internal static void Run()
        {
            Application.Run(new CatEditor());
        }
    }
}