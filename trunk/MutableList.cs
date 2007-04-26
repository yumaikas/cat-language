using System;

namespace Cat
{
    public abstract class FMutableList : FList 
    {
        public abstract void Set(int n, Object o);
        public abstract FMutableList Clone();
    }

    public class Bytes : FMutableList
    {
        #region fields
        byte[] m; 
        #endregion

        #region constructors
        public Bytes(int n)
        {
            m = new byte[n];
        }

        public Bytes(byte[] x)
        {
            m = x.Clone() as byte[];
        } 
        #endregion

        #region mutating function overrides 
        public override void Set(int n, Object o)
        {
            m[n] = (byte)o;
        }
        public override FMutableList Clone()
        {
            return new Bytes(m);
        }
        #endregion 

        #region abstract function overrides
        public override void ForEach(Accessor a)
        {
            foreach (object o in m)
                a(o);
        }

        public override FList GetIter()
        {
            throw new Exception("not implemented");
        }

        public override FList GotoNext()
        {
            throw new Exception("not implemented");
        }

        public override bool IsEmpty()
        {
            return Count() == 0;
        }

        public override int Count()
        {
            return m.Length;
        }

        public override Object Head()
        {
            return m[0];
        }
        #endregion 

        #region virtual function overrides
        public override object Nth(int n)
        {
            return m[n];
        }

        public override Object Last()
        {
            return m[Count() - 1];
        }

        public override FList DropN(int n)
        {
            Bytes ret = new Bytes(Count() - n);
            m.CopyTo(ret.m, n);
            return ret;
        }

        public override FList TakeN(int n)
        {
            Bytes ret = new Bytes(n);
            m.CopyTo(ret.m, 0);
            return ret;
        }

        public override FList TakeRange(int first, int count)
        {
            Bytes ret = new Bytes(count);
            m.CopyTo(ret.m, first);
            return ret;
        }

        public override FList DropWhile(FilterFxn f)
        {
            int n = CountWhile(f);
            return TakeRange(n, Count() - n);
        }

        public override int CountWhile(FilterFxn f)
        {
            for (int i = 0; i < Count(); ++i)
            {
                if (!f(Nth(i)))
                    return i;
            }
            return Count();
        }
        
        #endregion 
    }
}
