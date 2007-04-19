/// Public domain code by Christopher Diggins
/// http://www.cat-language.com

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Diagnostics;

namespace Cat
{    
    public delegate void Accessor(Object a);

    /// <summary>
    /// This is the base class for the primary Cat collection. There are 
    /// several different types of CatLists. This helps reduce copying 
    /// and reduce space requirements for certain kinds of lists.
    /// </summary>
    public abstract class CatList 
    {       
        private static EmptyList gNil = new EmptyList();

        #region static functions
        public static CatList nil() 
        { 
            return gNil; 
        }
        public static CatList unit(Object o) 
        { 
            return new UnitList(o); 
        }
        public static CatList pair(Object second, Object first) 
        { 
            return new ConsCell(unit(second), first); 
        }        
        public static CatList cons(CatList x, Object o) 
        { 
            return x.append(o); 
        }
        public static Object first(CatList x) 
        { 
            return x.head(); 
        }
        public static CatList rest(CatList x) 
        { 
            return x.tail(); 
        }        
        public static CatList map(CatList x, Function f)
        {
            return x.vmap(f);
        }
        public static Object fold(CatList x, Object o, Function f)
        {
            return x.vfold(o, f);
        }
        public static CatList filter(CatList x, Function f)
        {
            return x.vfilter(f);
        }
        public static CatList cat(CatList x, CatList y)
        {
            return x.vcat(y);
        }
        #endregion

        #region member functions
        public string str()
        {
            return ToString();
        }
        public CatStack ToCatStack()
        {
            CatStack stk = new CatStack();
            Accessor acc = delegate(Object a)
            { stk.PushFront(a); };
            WithEach(acc);
            return stk;
        }
        #endregion 

        #region abstract functions
        public abstract CatList dup();
        public abstract int count();
        public abstract Object nth(int n);
        public abstract CatList drop(int n);
        public abstract void WithEach(Accessor acc);
        #endregion

        #region virtual functions
        public virtual CatList vcat(CatList x)
        {
            CatStack stk = new CatStack();
            Accessor acc = delegate(Object a)
            { stk.PushFront(a); };
            WithEach(acc);
            x.WithEach(acc);
            return new StackToList(stk);
        }
        public virtual CatList vmap(Function f)
        {
            CatStack stk = new CatStack();
            Accessor acc = delegate(Object a)
            { stk.PushFront(f.Invoke(a)); };
            WithEach(acc);
            return new StackToList(stk);
        }
        public virtual Object vfold(Object x, Function f)
        {
            Accessor acc = delegate(Object a)
            { x = f.Invoke(x, a); };
            WithEach(acc);
            return x;
        }
        public virtual CatList vfilter(Function f)
        {
            CatStack stk = new CatStack();
            Accessor acc = delegate(Object a)
            { if ((bool)f.Invoke(a)) stk.PushFront(a); };
            WithEach(acc);
            return new StackToList(stk);
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
        public override string ToString()
        {
            string result = "( ";
            int nMax = count();
            if (nMax > 4) nMax = 4;

            for (int i = 0; i < nMax - 1; ++i)
            {
                result += MainClass.ObjectToString(nth(count() - (i + 1)));
                result += ", ";
            }

            if (nMax < count())
            {
                result += "..., ";
            }

            if (count() > 0)
                result += MainClass.ObjectToString(nth(0));
            result += ")";
            return result;
        }
        #endregion
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
        public override CatList vcat(CatList x)
        {
            return x;
        }
        public override CatList vmap(Function f) 
        { 
            return this; 
        }
        public override Object vfold(Object o, Function f) 
        { 
            return o; 
        }
        public override CatList vfilter(Function f)
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
        public override void WithEach(Accessor acc)
        {
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
        public override CatList vcat(CatList x)
        {
            return cons(x, m);
        }
        public override CatList vmap(Function f) 
        { 
            return unit(f.Invoke(m)); 
        }
        public override Object vfold(Object o, Function f) 
        { 
            return f.Invoke(o, m); 
        }
        public override CatList vfilter(Function f)
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
        public override void WithEach(Accessor acc)
        {
            acc(m);
        }
        #endregion
    }

    /// <summary>
    /// A ConsCell is a relatively naive implementation of a functional list
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
        public override void WithEach(Accessor acc)
        {
            acc(mHead);
            mTail.WithEach(acc);
        }
    }

    /// <summary>
    /// This simply is used to indicate that a list implementation
    /// has constant time complexity for count() and nth() methods
    /// </summary>
    public abstract class IndexableCatList : CatList
    {
    }

    /// <summary>
    /// Wraps a CatStack in a CatList
    /// </summary>
    public class StackToList : IndexableCatList
    {
        CatStack mStk;
        public StackToList(CatStack stk)
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
        public override void WithEach(Accessor acc)
        {
            for (int i = 0; i < count(); ++i)
                acc(nth(i));
        }
    }

    /// <summary>
    /// A SubList is a view into a ListFromStack. It is like a generalization
    /// of a ConsCell
    /// </summary>
    public class SubList : IndexableCatList
    {
        IndexableCatList mList;
        int mCount;
        int mOffset;
        
        public SubList(IndexableCatList list, int cnt)
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
        public override void WithEach(Accessor acc)
        {
            for (int i = 0; i < count(); ++i)
                acc(nth(i));
        }
    }

    /// <summary>
    /// Also known as a generator, a lazy list generates values as they are requested
    /// Some operations (such as "drop" and "dup" and "map) are always very fast, whereas count
    /// or nth will be O(n) complexity. A LazyList can be used to create infinite lists. For example all 
    /// positive even numbers can be expressed as "0 [true] [2 +] []" 
    /// </summary>
    public class LazyList : CatList 
    {
        Object moInit;
        Function mfNext;
        Function mfCond;
        Function mfMap;
        Function mfFilter;

        public LazyList(Object init, Function cond, Function next, Function filterf, Function mapf)
        {
            moInit = init;
            mfNext = next;
            mfCond = cond;
            mfMap = mapf;
            mfFilter = filterf;
        }

        public override CatList dup()
        {
            return new LazyList(moInit, mfCond, mfNext, mfFilter, mfMap);
        }

        public override int count()
        {
            int n = 0;
            Object o = moInit;
            while ((bool)mfCond.Invoke(o))
            {
                ++n;
                o = mfNext.Invoke(o);
            }
            return n;           
        }

        private Object nomap_nth(int n)
        {
            Object o = moInit;
            while ((bool)mfCond.Invoke(o))
            {
                if ((bool)mfFilter.Invoke(o))
                {
                    if (n-- == 0)
                        return o;
                }

                o = mfNext.Invoke(o);
            }
            throw new Exception("out of bounds");
        }

        public override Object nth(int n)
        {
            Object o = nomap_nth(n);
            return mfMap.Invoke(o);
        }
        public override CatList drop(int n)
        {
            return new LazyList(nomap_nth(n), mfCond, mfNext, mfFilter, mfMap);
        }
        public override Object head()
        {
            return nth(0);
        }
        public override CatList vmap(Function f)
        {
            return new LazyList(moInit, mfCond, mfNext, mfFilter, new ComposedFunction(mfMap, f));
        }
        public override CatList vfilter(Function f)
        {
            ComposedFunction mfNewFilter = new ComposedFunction(new ComposedFunction(mfFilter, f), new Primitives.And());
            return new LazyList(moInit, mfCond, mfNext, mfNewFilter, mfMap);
        }
        public override string ToString()
        {
            string result = ")";
            if ((bool)mfCond.Invoke(moInit))
            {
                result = nth(0).ToString() + result;
                Object next = mfNext.Invoke(moInit);
                if ((bool)mfCond.Invoke(next))
                {
                    result = ".. " + MainClass.ObjectToString(nth(1)) + ", " + result;
                }
            }
            result = "(" + result ;
            return result;
        }
        public override void WithEach(Accessor acc)
        {
            Object cur = moInit;
            while ((bool)mfCond.Invoke(cur))
            {
                if ((bool)mfFilter.Invoke(cur))
                    acc(mfMap.Invoke(cur));
                cur = mfNext.Invoke(cur);
            }
        }
    }    
}
