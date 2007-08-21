using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Diagnostics;

using Rtf;

namespace Cat
{
    public class TargetBase
    {
        public int Begin;
        public int End;
    }

    public class Target<T> : TargetBase
    {
        public T Data;
        public Target(int begin, T data)
        {
            Begin = begin;
            End = begin;
            Data = data;
        }
        public void SetEnd(int end)
        {
            Trace.Assert(end >= Begin);
            End = end;
        }

        public int GetLength()
        {
            return End - Begin;
        }
    }

    public class DefTarget : Target<DefinedFunction>
    {
        public DefTarget(int begin, DefinedFunction data)
            : base(begin, data)
        { }
    }

    public class CallTarget : Target<String>
    { 
        public CallTarget(int begin, String s)
            : base(begin, s)
        { }
    }

    /// <summary>
    /// Used to stream the abstract syntax tree to RTF format. This is designed for use in RTF editors.
    /// </summary>
    public class CatRtfWriter : CatWriter
    {
        RtfBuilder mRtf = new RtfBuilder();

        List<CallTarget> mFxnCalls = new List<CallTarget>();
        List<DefTarget> mFxnDefs = new List<DefTarget>();
        Dictionary<string, DefTarget> mFxnDefLookup = new Dictionary<string, DefTarget>();

        DefTarget mCurFxn;
        int mnPos = 0;
        int mnIndent = 0;
        bool mbShowCondensed = false;

        public CatRtfWriter()
        {
        }

        public void ShowCondensed(bool b)
        {
            mbShowCondensed = b;
        }

        public override void Clear()
        {
            mnPos = 0;
            mnIndent = 0;
            mRtf.Clear();
            mFxnCalls.Clear();
            mFxnDefs.Clear();
            mFxnDefLookup.Clear();
        }

        public string GetRtf(Font f)
        {
            return mRtf.ToRtf(f);
        }

        public CallTarget GetCallTargetFromPos(int pos)
        {
            return FindTarget<CallTarget>(mFxnCalls, pos, 0, mFxnCalls.Count - 1);
        }

        public DefTarget GetDefTargetFromPos(int pos)
        {
            return FindTarget<DefTarget>(mFxnDefs, pos, 0, mFxnDefs.Count - 1);
        }

        public DefinedFunction GetDefFromPos(int pos)
        {
            DefTarget t = GetDefTargetFromPos(pos);
            if (t == null) 
                return null;
            return t.Data;
        }

        public string GetNameFromCallPos(int pos)
        {
            CallTarget t = GetCallTargetFromPos(pos);
            if (t == null) return null;
            return t.Data;
        }

        public DefTarget GetDefTargetFromCallPos(int pos)
        {
            string s = GetNameFromCallPos(pos);
            if (s == null)
                return null;
            if (!mFxnDefLookup.ContainsKey(s))
                return null;
            return mFxnDefLookup[s];
        }

        public T FindTarget<T>(List<T> list, int pos, int begin, int end) where T : class
        {
            if (begin > end)
                return default(T);
            if (begin == end)
            {
                TargetBase t = list[begin] as TargetBase;
                if (pos >= t.Begin && pos <= t.End)
                    return t as T;
                return default(T);
            }
            else
            {
                int cnt = end - begin;
                int n = begin + (cnt / 2);
                TargetBase t = list[n] as TargetBase;
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
                    return t as T;
                }
            }
        }

        int GetCurPos()
        {
            return mnPos;
        }

        public override void StartFxnDef(DefinedFunction def)
        {
            Trace.Assert(mCurFxn == null);
            mCurFxn = new DefTarget(GetCurPos(), def);
            mFxnDefLookup.Add(def.GetName(), mCurFxn);
            mFxnDefs.Add(mCurFxn);
            mRtf.SetBold();
            mRtf.SetColor(Color.Crimson);
            Write("define ");
            mRtf.CloseTag();
            mRtf.CloseTag();
            Write(def.GetName());
        }

        public override void EndFxnDef()
        {
            mCurFxn.SetEnd(GetCurPos());
            mCurFxn = null;

            WriteLine();
            
            if (!mbShowCondensed)
                WriteLine();
        }

        void Write(string s)
        {
            mnPos += s.Length;
            mRtf.AddString(s);
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

        void WriteCall(string s)
        {
            CallTarget call = new CallTarget(GetCurPos(), s);
            Write(s);
            call.SetEnd(GetCurPos());
            mFxnCalls.Add(call);
        }

        void WriteIndent()
        {
            Write(new String(' ', mnIndent * 2));
        }

        public override void  WriteType(string s, bool bExplicit, bool bError)
        {
            Write(" : ");
            if (bExplicit)
            {
                mRtf.SetColor(Color.DodgerBlue);
            }
            else
            {
                mRtf.SetColor(Color.Gray);
                mRtf.SetItalic();
            }
            if (bError)
            {
                mRtf.SetColor(Color.RosyBrown);
                mRtf.SetEmboss();
                mRtf.SetWaveUnderline();
                mRtf.SetBold();
            }
            Write(s);
            if (bExplicit)
            {
                mRtf.CloseTag();
            }
            else
            {
                mRtf.CloseTag();
                mRtf.CloseTag();
            }
            if (bError)
            {
                mRtf.CloseTag();
                mRtf.CloseTag();
                mRtf.CloseTag();
                mRtf.CloseTag();
            }
        }

        public override void StartMetaBlock()
        {
            mRtf.SetItalic();
            mRtf.SetColor(Color.Gray);
            WriteLine("\n{{");
        }

        public override void EndMetaBlock()
        {
            Trace.Assert(mnIndent == 0);
            Write("}}");
            mRtf.CloseTag();
            mRtf.CloseTag();
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
            if (mbShowCondensed)
            {
                Write(" { ");
            }
            else
            {
                WriteLine("\n{");
                mnIndent++;
                WriteIndent();
            }
        }

        public override void EndImpl()
        {
            if (mbShowCondensed)
            {
                Write("}");
            }
            else
            {
                WriteLine();
                mnIndent--;
                WriteIndent();
                Write("}");
            }
        }

        public override void WriteMetaLabel(string s)
        {
            WriteIndent();
            WriteLine(s + ":");
        }

        public override void WriteMetaContent(string s)
        {
            WriteIndent();
            Write("  ");
            WriteLine(s);
        }

        public override void WritePrimitive(string s)
        {
            mRtf.SetColor(Color.DarkSlateBlue);
            Write(s + " ");
            mRtf.CloseTag();
        }

        public override void WriteInt(int x)
        {
            mRtf.SetColor(Color.DarkMagenta);
            Write(x + " ");
            mRtf.CloseTag();
        }

        public override void WriteDouble(double x)
        {
            mRtf.SetColor(Color.DarkOliveGreen);
            Write(x + " ");
            mRtf.CloseTag();
        }

        public override void WriteString(string x)
        {
            mRtf.SetColor(Color.DarkOrange);
            Write("\"" + x + "\" ");
            mRtf.CloseTag();
        }

        public override void WriteChar(char x)
        {
            mRtf.SetColor(Color.DarkKhaki);
            Write("'" + x + "' ");
            mRtf.CloseTag();
        }

        public override void WriteUnknown(string s)
        {
            Write(s + " ");
        }

        public override void StartQuotation()
        {
            if (mbShowCondensed)
            {
                Write("[ ");
            }
            else
            {
                WriteLine("[");
                mnIndent++;
                WriteIndent();
            }
        }

        public override void EndQuotation()
        {
            if (mbShowCondensed)
            {
                Write("] ");
            }
            else
            {
                WriteLine();
                mnIndent--;
                WriteIndent();
                WriteLine("]");
                WriteIndent();
            }
        }

        public override void WriteFunctionCall(DefinedFunction def)
        {
            mRtf.SetColor(Color.DarkOrchid);
            mRtf.SetUnderline();
            WriteCall(def.GetName());
            mRtf.CloseTag();
            mRtf.CloseTag();
            Write(" ");
        }
    }
}
