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
    public partial class CodeViewForm : Form
    {
        static CodeViewForm form = new CodeViewForm();

        CatSession mpSession;
        string msOptions;

        public CodeViewForm()
        {
            InitializeComponent();
        }

        private void CodeViewForm_Load(object sender, EventArgs e)
        {

        }

        public void Display(string sOptions, CatSession pSession)
        {
            mpSession = pSession;
            msOptions = sOptions;
            ShowDialog();
        }

        public static void Show(string sOptions)
        {
            form.Display(sOptions, CatSession.GetGlobalSession());
        }

        private void mEdit_TextChanged(object sender, EventArgs e)
        {

        }

        private void CodeViewForm_Shown(object sender, EventArgs e)
        {
            mEdit.Clear();
            CatTextEditWriter w = new CatTextEditWriter(mEdit);
            mpSession.Output(w);
        }
    }

    public class CatTextEditWriter : CatWriter
    {
        RichTextBox mEdit;
        List<Target> mTokens = new List<Target>();
        List<Target> mFxns = new List<Target>();
        Target mCurFxn;
        int mnIndent;

        public CatTextEditWriter(RichTextBox edit)
        {
            mEdit = edit;
        }

        public class Target
        {
            public int Begin;
            public int End;
            public Object Data;
            public Target(int begin, Object data)
                : this(begin, begin, data)
            {
            }
            public Target(int begin, int end, Object data)
            {
                Trace.Assert(end >= begin);
                Begin = begin;
                End = end;
                Data = data;
            }
            public void SetEnd(int end)
            {
                Trace.Assert(end >= Begin);
                End = end; 
            }
        }

        public Target TokenFromPos(int pos)
        {
            return FindTarget(mTokens, pos, 0, mTokens.Count - 1);
        }

        public DefinedFunction FunctionFromPos(int pos)
        {
            Target t = FindTarget(mFxns, pos, 0, mFxns.Count - 1);
            if (t == null) return null;
            return t.Data as DefinedFunction;
        }

        public Target FindTarget(List<Target> list, int pos, int begin, int end)
        {
            if (begin > end)
            {
                return null;
            }
            else 
            if (begin == end)
            {
                Target t = list[begin];
                if (pos >= t.Begin && pos <= t.End)
                    return t;
                return null;
            }
            else 
            {
                int cnt = end - begin;
                int n = begin + (cnt / 2);
                Target t = list[n];
                if (pos < t.Begin)
                {
                    return FindTarget(list, pos, begin, n - 1);
                }
                else if (pos > t.End)
                {
                    return FindTarget(list, pos, n + 1, end);
                }
                else
                {
                    return t;
                }
            }
        }

        int GetCurPos()
        {
            return mEdit.SelectionStart;
        }

        public override void StartFxnDef(DefinedFunction def) 
        {
            Trace.Assert(mCurFxn == null);
            mCurFxn = new Target(GetCurPos(), def);
            mFxns.Add(mCurFxn);
            WriteLine("define " + def.GetName());
        }

        public override void EndFxnDef() 
        {
            mCurFxn.SetEnd(GetCurPos());
            mCurFxn = null;
        }
 
        void Write(string s) 
        {
            mEdit.AppendText(s);
        }

        void WriteLine(string s) 
        {
            Write(s);
            WriteLine();
        }

        void WriteLine()
        {
            Write("\n");
        }

        void WriteIndent()
        {
            Write(new String(' ', mnIndent * 2));
        }
 
        public override void WriteType(string s) 
        {
            WriteLine("   : " + s);
        }

        public override void StartMetaBlock() 
        {
            WriteLine("{{");
        }

        public override void EndMetaBlock() 
        {
            Trace.Assert(mnIndent == 0);
            WriteLine("}}");
        }

        public override void StartMetaNode() 
        {
            mnIndent++;
        }

        public override void EndMetaNode() 
        {
            mnIndent--;
        }

        public override void StartImpl() 
        {
            WriteLine("{");
            mnIndent++;
            WriteIndent();
        }

        public override void EndImpl() 
        {
            WriteLine();
            mnIndent--;
            WriteIndent();
            WriteLine("}");
        }
        
        public override void WriteMetaLabel(string s) 
        {
            WriteIndent();
            WriteLine(s);
        }

        public override void WriteMetaContent(string s) 
        {
            WriteIndent();
            Write("  ");
            WriteLine(s);
        }

        public override void WritePrimitive(string s) 
        {
            Write(s + " ");
        }

        public override void WriteNumber(string s) 
        {
            Write(s + " ");
        }

        public override void WriteString(string s) 
        {
            Write(s + " ");
        }

        public override void WriteChar(string s) 
        {
            Write(s + " ");
        }

        public override void WriteUnknown(string s) 
        {
            Write(s + " ");
        }

        public override void StartQuotation() 
        {
            WriteLine("");
            WriteIndent();
            WriteLine("[");
            mnIndent++;
            WriteIndent();
        }

        public override void EndQuotation() 
        {
            WriteLine();
            mnIndent--;
            WriteLine("]");
            WriteIndent();
        }

        public override void WriteFunctionCall(DefinedFunction def) 
        {
            Write(def.GetName() + " ");
        }
    }
}