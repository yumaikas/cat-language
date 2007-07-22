using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Cat
{
    public abstract class CatWriter
    {
        public void WriteFunction(DefinedFunction def)
        {
            StartFxnDef(def);
            if (def.GetFxnType() != null)
                WriteType(def.GetFxnTypeString());
            if (def.HasMetaData())
            {
                StartMetaBlock();
                CatMetaDataBlock block = def.GetMetaData();
                foreach (CatMetaData child in block)
                    WriteMetaData(child);
                EndMetaBlock();
            }
            StartImpl();
            foreach (Function f in def.GetChildren())
                WriteTerm(f);
            EndImpl();
            EndFxnDef();
        }

        public void WriteTerm(Function f)
        {
            if (f is DefinedFunction)
                WriteFunctionCall(f as DefinedFunction);
            else if (f is PushValue<int>)
                WriteNumber(f.ToString());
            else if (f is PushValue<char>)
                WriteChar(f.ToString());
            else if (f is PushValue<string>)
                WriteString(f.ToString());
            else if (f is PushValue<int>)
                WriteNumber(f.ToString());
            else if (f is Quotation)
                WriteQuotation(f as Quotation);
            else if (f is PrimitiveFunction)
                WritePrimitive(f.ToString());
            else
                WriteUnknown(f.ToString());
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
            WriteMetaContent(mb.GetContent());
            foreach (CatMetaData child in mb)
                WriteMetaData(child);
            EndMetaNode();
        }

        public abstract void StartFxnDef(DefinedFunction def);
        public abstract void EndFxnDef();
        public abstract void WriteType(string s);
        public abstract void StartMetaBlock();
        public abstract void EndMetaBlock();
        public abstract void StartMetaNode();
        public abstract void EndMetaNode();
        public abstract void StartImpl();
        public abstract void EndImpl();
        public abstract void WriteMetaLabel(string s);
        public abstract void WriteMetaContent(string s);
        public abstract void WritePrimitive(string s);
        public abstract void WriteNumber(string s);
        public abstract void WriteString(string s);
        public abstract void WriteChar(string s);
        public abstract void WriteUnknown(string s);
        public abstract void StartQuotation();
        public abstract void EndQuotation();
        public abstract void WriteFunctionCall(DefinedFunction def);
    }
}
