using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Cat
{
    public abstract class CatWriter
    {
        protected bool mbShowComments = true;
        protected bool mbShowTypes = true;
        protected bool mbShowInferredTypes = true;
        protected bool mbShowImplementation = true;

        public void WriteFunction(DefinedFunction def)
        {
            StartFxnDef(def);
            if (mbShowTypes)
            {
                bool bExplicit = def.IsTypeExplicit();
                bool bError = def.HasTypeError();
                if (mbShowInferredTypes || bExplicit)
                {
                    WriteType(def.GetFxnTypeString(), bExplicit, bError);
                }
            }
            if (def.HasMetaData() && mbShowComments)
            {
                StartMetaBlock();
                CatMetaDataBlock block = def.GetMetaData();
                foreach (CatMetaData child in block)
                    WriteMetaData(child);
                EndMetaBlock();
            }
            if (mbShowImplementation)
            {
                StartImpl();
                foreach (Function f in def.GetChildren())
                    WriteTerm(f);
                EndImpl();
            }
            EndFxnDef();
        }

        public void WriteTerm(Function f)
        {
            if (f is DefinedFunction)
                WriteFunctionCall(f as DefinedFunction);
            else if (f is PushValue<char>)
                WriteChar((f as PushValue<char>).GetValue());
            else if (f is PushValue<string>)
                WriteString((f as PushValue<string>).GetValue());
            else if (f is PushValue<int>)
                WriteInt((f as PushValue<int>).GetValue());
            else if (f is PushValue<double>)
                WriteDouble((f as PushValue<double>).GetValue());
            else if (f is Quotation)
                WriteQuotation(f as Quotation);
            else if (f is PrimitiveFunction)
                WritePrimitive(f.GetName());
            else
                WriteUnknown(f.GetName());
        }

        public void WriteQuotation(Quotation q)
        {
            StartQuotation();
            foreach (Function f in q.GetChildren())
                WriteTerm(f);
            EndQuotation();
        }

        public void WriteMetaData(CatMetaData mb)
        {
            StartMetaNode();
            WriteMetaLabel(mb.GetLabel());
            string sContent = mb.GetContent();
            if (sContent.Length > 0)
                WriteMetaContent(sContent);
            foreach (CatMetaData child in mb)
                WriteMetaData(child);
            EndMetaNode();
        }

        public void ShowComments(bool b)
        {
            mbShowComments = b;
        }

        public void ShowTypes(bool b)
        {
            mbShowTypes = b;
        }

        public void ShowInferredTypes(bool b)
        {
            mbShowInferredTypes = b;
        }

        public void ShowImplementation(bool b)
        {
            mbShowImplementation = b;
        }

        public abstract void Clear();      
        public abstract void StartFxnDef(DefinedFunction def);
        public abstract void EndFxnDef();
        public abstract void WriteType(string s, bool bExplicit, bool bError);
        public abstract void StartMetaBlock();
        public abstract void EndMetaBlock();
        public abstract void StartMetaNode();
        public abstract void EndMetaNode();
        public abstract void StartImpl();
        public abstract void EndImpl();
        public abstract void WriteMetaLabel(string s);
        public abstract void WriteMetaContent(string s);
        public abstract void WritePrimitive(string s);
        public abstract void WriteInt(int x);
        public abstract void WriteDouble(double x);
        public abstract void WriteString(string x);
        public abstract void WriteChar(char x);
        public abstract void WriteUnknown(string s);
        public abstract void StartQuotation();
        public abstract void EndQuotation();
        public abstract void WriteFunctionCall(DefinedFunction def);
    }
}
