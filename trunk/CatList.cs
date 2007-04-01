using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Diagnostics;

namespace Cat
{    
    /// <summary>
    /// This is the base class for the primary Cat collection. There are 
    /// several different types of CatLists. The different kinds of lists, 
    /// improve performance.
    /// </summary>
    public abstract class CatList 
    {       
        private static EmptyList gNil = new EmptyList();
        public static EmptyList nil() { return gNil; }
        public static UnitList unit(Object o) { return new UnitList(o); }
        
        // These are static functions which remove the original argument, 
        // Notice that if I want to eventually use reference counting, I will 
        // have to modify these functions
        public static CatList cons(CatList x, Object o) { return x.append(o); }
        public static CatList cdr(CatList x) { return x.tail(); }        

        #region abstract functions        
        public abstract CatList dup();
        public abstract int count();
        public abstract Object nth(int n);
        public abstract CatList drop(int n);
        #endregion

        public virtual CatList map(Function f)
        {
            CatStack stk = new CatStack();
            for (int i = count(); i > 0; --i)
            {
                Object tmp = nth(i - 1);
                stk.Push(f.Invoke(tmp));
            }
            return new ListFromStack(stk);
        }
        public virtual Object foldl(Object x, Function f)
        {
            for (int i = 0; i < count(); ++i)
            {
                Object tmp = nth(i);
                x = f.Invoke(x, tmp);
            }
            return x;
        }
        public virtual CatList filter(Function f)
        {
            CatStack stk = new CatStack();
            for (int i = count(); i > 0; --i)
            {
                Object tmp = nth(i - 1);
                if ((bool)f.Invoke(tmp))
                    stk.Push(tmp);
            }
            return new ListFromStack(stk);
        }
        public virtual CatList append(Object o) 
        { 
            return new ConsCell(this, o); 
        }
        public virtual Object head()
        {
            return nth(count() - 1);
        }

        public virtual CatList tail()
        {
            return drop(1);
        }

        public string str()
        {
            return ToString();
        }

        public override string ToString()
        {
            string result = "( ";
            int nMax = count();
            if (nMax > 4) nMax = 4;

            for (int i = 0; i < nMax - 1; ++i)
            {
                result += nth(i).ToString();
                result += ", ";
            }

            if (nMax < count())
            {
                result += "... ";
            }

            if (count() > 0)
                result += nth(count() - 1).ToString();
            result += ")";
            return result;
        }
    }

    /// <summary>
    /// An EmptyList is a special case of a CatList with no items
    /// </summary>
    public class EmptyList : CatList
    {
        #region public functions
        public override CatList append(Object o) 
        { 
            return unit(o); 
        }
        public override CatList dup() 
        { 
            return this; 
        }
        public override int count() 
        { 
            return 0; 
        }
        public override CatList map(Function f) 
        { 
            return this; 
        }
        public override Object foldl(Object o, Function f) 
        { 
            return o; 
        }
        public override CatList filter(Function f)
        {
            return this;
        }
        public override Object nth(int n) 
        { 
            throw new Exception("no items"); 
        }
        public override CatList drop(int n) 
        { 
            if (n != 0) 
                throw new Exception("no items"); 
            return this; 
        }
        public override object head()
        {
            throw new Exception("empty list, no head");
        }
        public override CatList tail()
        {
            throw new Exception("empty list, no tail");
        }
        #endregion
    }

    /// <summary>
    /// A UnitList is a special case of a CatList with one item
    /// </summary>
    public class UnitList : CatList
    {
        private Object m;
        public UnitList(Object o) { m = o; } 

        #region public functions
        public override CatList dup() 
        { 
            return unit(m); 
        }
        public override int count() 
        { 
            return 1; 
        }
        public override CatList map(Function f) 
        { 
            return unit(f.Invoke(m)); 
        }
        public override Object foldl(Object o, Function f) 
        { 
            return f.Invoke(o, m); 
        }
        public override CatList filter(Function f)
        {
            if ((bool)f.Invoke(m))
                return unit(m);
            else
                return nil();
        }
        public override Object nth(int n) 
        { 
            if (n != 0) 
                throw new Exception("only one item in list"); 
            return m; 
        }
        public override CatList drop(int n) 
        {
            switch (n)
            {
                case 0: return this;
                case 1: return nil();
                default: throw new Exception("list only has one item");
            }
        }
        public override object head()
        {
            return m;
        }
        public override CatList tail()
        {
            return nil();
        }
        #endregion
    }

    /// <summary>
    /// A ConsCell is a very naive implementation of a functional list
    /// </summary>
    public class ConsCell : CatList
    {
        Object mHead;
        CatList mTail;
        public ConsCell(CatList list, Object o)
        {
            mHead = o;
            mTail = list;
        }
        public override CatList dup() 
        { 
            return this; 
        }
        public override int count() 
        { 
            return 1 + mTail.count(); 
        }
        public override CatList map(Function f)
        {
            return cons(mTail.map(f), f.Invoke(mHead));
        }
        public override Object foldl(Object o, Function f)
        {
            return f.Invoke(o, mTail.foldl(mHead, f));
        }
        public override CatList filter(Function f)
        {
            if ((bool)f.Invoke(mHead))
                return cons(mTail.filter(f), mHead);
            else
                return mTail.filter(f);
        }
        public override Object nth(int n) 
        { 
            if (n == 0) 
                return mHead; 
            else 
                return mTail.nth(n - 1); 
        }
        public override CatList drop(int n) 
        { 
            if (n == 0) 
                return this; 
            else 
                return mTail.drop(n - 1); 
        }
        public override object head()
        {
            return mHead;
        }
        public override CatList tail()
        {
            return mTail;
        }
    }

    /// <summary>
    /// Wraps a CatStack in a CatList
    /// </summary>
    public class ListFromStack : CatList
    {
        CatStack mStk;
        public ListFromStack(CatStack stk)
        {
            mStk = stk;
        }
        public override CatList dup() 
        { 
            return this; 
        }
        public override int count() 
        { 
            return mStk.Count; 
        }
        public override Object nth(int n) 
        { 
            return mStk[n]; 
        }
        public override CatList drop(int n) 
        {
            if (n == 0) 
                return this;
            
            switch (count() - n)
            {
                case 0:
                    return nil();
                case 1:
                    return unit(mStk[count() - 1]);
                default:
                    return new SubList(this, count() - n);
            }
        }
    }

    /// <summary>
    /// A SubList is a view into a ListFromStack. It is like a generalization
    /// of a ConsCell
    /// </summary>
    public class SubList : CatList
    {
        CatList mList;
        int mCount;
        int mOffset;
        public SubList(CatList list, int cnt)
        {
            Trace.Assert(cnt >= 2);
            Trace.Assert(cnt < list.count());
            mList = list;
            mCount = cnt;
            mOffset = list.count() - cnt;
        }
        public override CatList dup() 
        { 
            return this; 
        }
        public override int count() 
        { 
            return mCount; 
        }
        public override Object nth(int n) 
        {
            Trace.Assert(mOffset + mCount == mList.count());
            if (n >= mCount) 
                throw new Exception("index out of range"); 
            return mList.nth(n + mOffset); 
        }
        public override CatList drop(int n)
        {
            if (n == 0)
                return this;

            switch (count() - n)
            {
                case 0:
                    return nil();
                case 1:
                    return unit(nth(mCount - 1));
                default:
                    return new SubList(mList, mCount - n);
            }
        }
    }
    
    public class LazyList : CatList 
    {
        Object mInit;
        Function mNext;
        Function mCond;

        public LazyList(Object init, Function cond, Function next)
        {
            mInit = init;
            mNext = next;
            mCond = cond;
        }

        public override CatList dup()
        {
            return new LazyList(mInit, mCond, mNext);
        }

        public override int count()
        {
            int n = 0;
            Object o = mInit;
            while ((bool)mCond.Invoke(o))
            {
                ++n;
                o = mNext.Invoke(o);
            }
            return n;           
        }

        public override Object nth(int n)
        {
            Object o = mInit;
            while ((bool)mCond.Invoke(o))
            {
                if (n-- == 0)
                    return o;
                o = mNext.Invoke(o);
            }
            throw new Exception("out of bounds");        
        }

        public override CatList drop(int n)
        {
            return new LazyList(nth(n), mNext, mCond);
        }
        public override Object head()
        {
            return mInit;
        }
        public override string ToString()
        {
            // TODO: something more descriptive
            return "(" + mInit.ToString() + "..)";
        }       
    }
}
