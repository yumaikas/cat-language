/// Dedicated to the public domain by Christopher Diggins
/// http://creativecommons.org/licenses/publicdomain/

using System;

namespace Cat
{
    public abstract class FMutableList : FList 
    {
        public abstract void Set(int n, Object o);
        public abstract FMutableList Clone();
    }

    public class MArray<T> : FMutableList
    {
        #region fields
        public T[] m; 
        #endregion

        #region constructors
        public MArray(int n)
        {
            m = new T[n];
        }

        public MArray(T[] x)
        {
            m = x.Clone() as T[];
        }

        public MArray(FList f)
        {
            if (f.IsKnownInfinite())
                throw new Exception("Can not create a mutable copy of an infinite list");
            int n = f.Count();
            m = new T[n];
            FList iter = f.GetIter();
            int i = 0;
            while (!iter.IsEmpty())
            {
                m[i++] = (T)iter.GetHead();
                iter = iter.GotoNext();
            }
        }
        #endregion

        #region mutating function overrides 
        public override void Set(int n, Object o)
        {
            m[n] = (T)o;
        }
        public override FMutableList Clone()
        {
            return new MArray<T>(m);
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
            return new RangedArray<T>(m, 0, m.Length);
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

        public override Object GetHead()
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
            MArray<T> ret = new MArray<T>(Count() - n);
            m.CopyTo(ret.m, n);
            return ret;
        }

        public override FList TakeN(int n)
        {                         
            MArray<T> ret = new MArray<T>(Count() < n ? Count() : 5);
            m.CopyTo(ret.m, 0);
            return ret;
        }

        public override FList TakeRange(int first, int count)
        {
            MArray<T> ret = new MArray<T>(count);
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

    public class ByteBlock : MArray<byte>
    {
        public ByteBlock(int n) : base(n) { }
        public ByteBlock(byte[] x) : base(x) { }
        public ByteBlock(FList f) : base(f) { }

        public void ZeroMemory()
        {
            m.Initialize();
        }
    }
}
