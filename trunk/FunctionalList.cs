/// Public domain code by Christopher Diggins
/// http://www.cat-language.com

using System;

namespace Cat
{
    #region delegate types
    public delegate void Accessor(object o);
    public delegate bool PairAccessor(object x, object y);
    public delegate object FoldFxn(object x, object y);
    public delegate object MapFxn(object o);
    public delegate object RangeGenFxn(int n);
    public delegate bool FilterFxn(object o);
    #endregion

    /// <summary>
    /// This is a quasi-functional list. It implements common functional list operations in a non-mutable 
    /// manner and behaves as a mutable iterator.  
    /// There are several different specializations of each class which reduce the complexity of many 
    /// common algorithms, and offer optimized versions of the classes for different scenarios. 
    /// </summary>
    public abstract class FList
    {
        #region abstract functions
        public abstract void ForEach(Accessor a);
        public abstract FList GetIter();
        public abstract FList GotoNext();
        public abstract object GetHead();
        public abstract bool IsEmpty();
        #endregion

        #region static delegate functions
        public static FilterFxn ComposeFilters(FilterFxn f, FilterFxn g)
        {
            return delegate(object x) { return f(x) && g(x); };
        }
        public static FilterFxn NegateFilter(FilterFxn f)
        {
            return delegate(object x) { return !f(x); };
        }
        #endregion

        #region static data
        static EmptyList nil = new EmptyList();
        #endregion

        #region static functions
        public static FList Concat(FList first, FList second) 
        {
            if (second.IsEmpty())
                return first;
            if (first.IsEmpty())
                return second;
            return new ConcatPair(first, second); 
        }

        public static FList Gen(object o, MapFxn next, FilterFxn cond)
        {
            return new Generator(o, next, cond);
        }

        public static FList RangeGen(RangeGenFxn f, int first, int count)
        {
            return new RangeGenerator(f, first, count);
        }

        public static FList MakeRepeater(Object o)
        {
            return new FListRepeater(o);
        }

        public static FList Nil()
        {
            return nil;
        }

        public static FList MakeUnit(object x)
        {
            return new Unit(x);
        }

        public static FList MakePair(object first, object second)
        {
            return new Pair(first, second);
        }

        public static FList Cons(object x, FList list)
        {
            return new ConsCell(x, list);
        }

        public static void PairwiseForEach(PairAccessor f, FList x, FList y)
        {
            if (x.IsEmpty() || y.IsEmpty()) return;
            if (f(x.GetHead(), y.GetHead())) return;
            PairwiseForEach(f, x.Tail(), y.Tail());
        }

        public static bool AreListsEqual(FList x, FList y)
        {
            // Same memory address? 
            if (x == y) return true;

            // Known finite lists, and known infinite lists are never equal.
            if (x.IsKnownFinite() && y.IsKnownInfinite()) return false;
            if (x.IsKnownInfinite() && y.IsKnownFinite()) return false;
            
            // If either list is empty, then we can simply look to see if the other one is as well
            if (x.IsEmpty()) 
                return y.IsEmpty();
            if (y.IsEmpty())
                return false; // since we know from the previous condition that both top and y aren't empty.

            // Compare the count if it is easy.
            if (x.IsKnownFinite() && y.IsKnownFinite())
                if (x.Count() != y.Count()) return false;

            // We have to resort to pairwise comparisons
            bool ret = true;            
            PairAccessor f = delegate(Object first, Object second)
            {
                if (!first.Equals(second))
                    return ret = false;
                return true;
            };
            PairwiseForEach(f, x, y);
            return ret;
        }
        #endregion

        #region virtual functions
        public override bool Equals(object obj)
        {
            if (!(obj is FList)) return false;
            return AreListsEqual(this, obj as FList);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public virtual int Count() 
        {
            int result = 0;
            ForEach(delegate(object o) { result += 1; });
            return result;
        }

        public virtual bool IsKnownFinite()
        {
            // most lists are known to be finite, so this is the default
            return true;
        }

        public virtual bool IsKnownInfinite()
        {
            // most lists are finite
            return false;
        }

        public virtual object Fold(object init, FoldFxn f)
        {
            ForEach(delegate(object o) { init = f(init, o); });
            return init;
        }

        public virtual FList Map(MapFxn f)
        {
            return new MappedFList(this, f);
        }

        public virtual object Nth(int n)
        {
            FList iter = GetIter();
            while (!iter.IsEmpty() && n != 0)
            {
                iter = iter.GotoNext();
                --n;
            }
            if (n != 0) throw new Exception("out of range");
            return iter.GetHead();
        }

        public virtual FList Tail()
        {
            return GetIter().GotoNext();
        }

        public virtual Object Last()
        {
            Object result = null;
            ForEach(delegate(Object o) { result = o; });
            return result;
        }

        public virtual FList Filter(FilterFxn f)
        {
            return new FilteredFList(this, f);
        }

        public virtual FList DropN(int n)
        {
            FList ret = GetIter();
            while (!ret.IsEmpty() && n != 0)
            {
                ret = ret.GotoNext();
                --n; 
            }
            return ret;
        }

        public virtual FList TakeN(int n)
        {
            switch (n)
            {
                case (0):
                    return Nil();
                case (1):
                    return MakeUnit(GetHead());
                case (2):
                    return MakePair(GetHead(), Tail().GetHead());
            }

            object[] a = new object[n];
            int i = 0;
            FList iter = GetIter();
            while (!iter.IsEmpty() && (i < n))
            {
                a[i++] = iter.GetHead();
                iter = iter.GotoNext();
            }
            if (i < n)
            {
                return new RangedArray<object>(a, 0, i);
            }
            else
            {
                return new FArray<object>(a);
            }
        }

        public virtual FList TakeRange(int first, int count)
        {
            return DropN(first).TakeN(count);
        }

        public virtual FList TakeWhile(FilterFxn f)
        {
            int n = CountWhile(f);
            return TakeN(n);
        }

        public virtual FList DropWhile(FilterFxn f)
        {
            FList ret = GetIter();
            while (!ret.IsEmpty() && f(ret.GetHead()))
                ret = ret.GotoNext();
            return ret;
        }

        public virtual int CountWhile(FilterFxn f)
        {
            int cnt = 0;
            FList ret = GetIter();
            while (!ret.IsEmpty() && f(ret.GetHead()))
            {
                ret = ret.GotoNext();
                ++cnt;
            }
            return cnt;
        }

        public virtual FList Flatten()
        {
            return new FlattenedFList(this);
        }
        #endregion
    }

    public class EmptyList : FList
    {
        #region abstract function overrides
        public override void ForEach(Accessor a)
        {
        }

        public override FList GetIter()
        {
            return this;
        }

        public override FList GotoNext()
        {
            return this;
        }

        public override object GetHead()
        {
            throw new Exception("empty list");
        }

        public override bool IsEmpty()
        {
            return true;
        }
        #endregion 

        #region virtual function overrides
        public override int Count()
        {
            return 0;
        }

        public override int CountWhile(FilterFxn f)
        {
            return 0;
        }

        public override FList TakeN(int n)
        {
            return this;
        }

        public override FList DropN(int n)
        {
            return this;
        }

        public override FList TakeRange(int first, int count)
        {
            return this;
        }

        public override FList TakeWhile(FilterFxn f)
        {
            return this;
        }

        public override FList DropWhile(FilterFxn f)
        {
            return this;
        }

        public override FList Filter(FilterFxn f)
        {
            return this;
        }

        public override FList Map(MapFxn f)
        {
            return this;
        }

        public override object Fold(object init, FoldFxn f)
        {
            return init;
        }

        public override object Nth(int n)
        {
            throw new Exception("empty list");
        }

        public override object Last()
        {
            throw new Exception("empty list");
        }

        public override FList Tail()
        {
            return this;
        }
        #endregion
    }

    public class Unit : FList
    {
        #region fields
        object m; 
        #endregion

        #region constructors
        public Unit(object x)
        {
            m = x;
        } 
        #endregion

        #region abstract function overrides
        public override void ForEach(Accessor a)
        {
            a(m);
        }

        public override FList GetIter()
        {
            return this;
        }

        public override FList GotoNext()
        {
            return Nil();
        }

        public override bool IsEmpty()
        {
            return false;
        }

        public override object GetHead()
        {
            return m;
        }
        #endregion 

        #region virtual function overrides
        public override int Count()
        {
            return 1;
        }

        public override int CountWhile(FilterFxn f)
        {
            return f(m) ? 1 : 0;
        }

        public override object Last()
        {
            return m;
        }

        public override FList Tail()
        {
            return Nil();
        }

        public override FList Map(MapFxn f)
        {
            return MakeUnit(f(m));
        }

        public override FList Filter(FilterFxn f)
        {
            if (f(m))
            {
                return this;
            }
            else
            {
                return Nil();
            }
        }

        public override object Fold(object init, FoldFxn f)
        {
            return f(init, m);
        }

        public override FList DropN(int n)
        {
            if (n == 0) return this;
            else return Nil();
        }
    
        public override FList TakeN(int n)
        {
            if (n == 0) return Nil();
            else return this;
        }

        public override FList DropWhile(FilterFxn f)
        {
            if (f(m)) return Nil();
            else return this;
        }

        public override FList TakeWhile(FilterFxn f)
        {
            if (f(m)) return this;
            else return Nil();
        }

        public override object Nth(int n)
        {
            if (n != 0) throw new Exception("out of range");
            return m;
        }
        #endregion
    }

    public class Pair : FList
    {
        #region fields
        public object mFirst;
        public object mSecond; 
        #endregion

        #region constructors
        public Pair(object first, object second)
        {
            mFirst = first;
            mSecond = second;
        } 
        #endregion

        #region abstract function overrides
        public override void ForEach(Accessor a)
        {
            a(mFirst);
            a(mSecond);
        }

        public override FList GetIter()
        {
            return this;
        }

        public override FList GotoNext()
        {
            return MakeUnit(mSecond);
        }

        public override bool IsEmpty()
        {
            return false;
        }

        public override object GetHead()
        {
            return mFirst;
        }
        #endregion

        #region virtual function overrides
        public override object Last()
        {
            return mSecond;
        }

        public override FList Tail()
        {
            return MakeUnit(mSecond);
        }

        public override int Count()
        {
            return 2;
        }

        public override int CountWhile(FilterFxn f)
        {
            return (f(mFirst) ? 1 : 0) + (f(mSecond) ? 1 : 0);
        }

        public override FList Map(MapFxn f)
        {
            return MakePair(f(mFirst), f(mSecond));
        }

        public override FList Filter(FilterFxn f)
        {
            if (f(mFirst))
            {
                if (f(mSecond))
                    return this;
                else
                    return MakeUnit(mFirst);
            }
            else
            {
                if (f(mSecond))
                    return MakeUnit(mSecond);
                else
                    return Nil();
            }
        }

        public override object Fold(object init, FoldFxn f)
        {
            return f(f(init, mFirst), mSecond);
        }

        public override FList DropN(int n)
        {
            switch (n)
            {
                case (0) : 
                    return this;
                case (1) :
                    return MakeUnit(mSecond);
                default :
                    return Nil();
            }
        }

        public override FList TakeN(int n)
        {
            switch (n)
            {
                case (0):
                    return Nil();
                case (1):
                    return MakeUnit(mFirst);
                default:
                    return this;
            }
        }

        public override object Nth(int n)
        {
            switch (n)
            {
                case (0):
                    return mFirst;
                case (1):
                    return mSecond;
                default:
                    throw new Exception("out of range");
            }
        }

        public override FList DropWhile(FilterFxn f)
        {
            if (!f(mFirst)) return this;
            if (!f(mSecond)) return MakeUnit(mSecond);
            return Nil();
        }

        public override FList TakeWhile(FilterFxn f)
        {
            if (!f(mFirst)) return Nil();
            if (!f(mSecond)) return MakeUnit(mFirst);
            return this;
        }
        #endregion

        #region other functions
        public object First()
        {
            return mFirst;
        }
        public object Second()
        {
            return mSecond;
        }
        #endregion
    }

    public class ConcatPair : FList
    {
        #region fields
        FList mFirst;
        int mFirstCount;
        FList mSecond; 
        #endregion

        #region constructors
        public ConcatPair(FList first, FList second)
            : this(first, second, first.Count())
        {
        }

        public ConcatPair(FList first, FList second, int firstCount)
        {
            mFirst = first;
            mFirstCount = firstCount;
            mSecond = second;
        } 
        #endregion

        #region abstract function overrides
        public override void ForEach(Accessor a)
        {
            mFirst.ForEach(a);
            mSecond.ForEach(a);
        }

        public override FList GetIter()
        {
            if (mFirstCount == 0)
            {
                return mSecond.GetIter();
            }
            else
            {
                return new ConcatPair(mFirst.GetIter(), mSecond, mFirstCount);
            }
        }

        public override FList GotoNext()
        {
            if (mFirstCount == 0)
                throw new Exception("internal error, corrupt ConcatPair");

            if (mFirstCount == 1)
            {
                return mSecond.GetIter();
            }
            else
            {
                mFirst = mFirst.GotoNext();
                --mFirstCount;
            }
            return this;
        }

        public override bool IsEmpty()
        {
            return mFirst.IsEmpty() && mSecond.IsEmpty();
        }

        public override Object GetHead()
        {
            if (mFirstCount != 0)
                return mFirst.GetHead();
            else
                return mSecond.GetHead();
        }
        #endregion

        #region abstract function overrides
        public override int Count()
        {
            return mFirstCount + mSecond.Count();
        }

        public override int CountWhile(FilterFxn f)
        {
            return mFirst.CountWhile(f) + mSecond.CountWhile(f);
        }

        public override object Nth(int n)
        {
            if (n < mFirstCount)
                return mFirst.Nth(n);
            else
                return mSecond.Nth(n - mFirstCount);
        }

        public override FList Tail()
        {
            if (mFirst.IsEmpty())
                return mSecond.Tail();
            return Concat(mFirst.Tail(), mSecond);
        }

        public override Object Last()
        {
            return mSecond.Last();
        }

        /// <summary>
        /// Overriding Filter allows concatenation to retain some of its optimizations.
        /// </summary>
        public override FList Filter(FilterFxn f)
        {
            return new ConcatPair(new FilteredFList(mFirst, f), new FilteredFList(mSecond, f));
        }

        /// <summary>
        /// Overriding Map allows concatenation to retain some of its optimizations.
        /// </summary>
        public override FList Map(MapFxn f)
        {
            return Concat(new MappedFList(mFirst, f), new MappedFList(mSecond, f));
        }

        public override FList DropN(int n)
        {
            if (n >= mFirstCount)
            {
                return mSecond.DropN(n - mFirstCount);
            }
            else
            {
                return Concat(mFirst.DropN(n), mSecond);
            }
        }

        public override FList TakeN(int n)
        {
            if (n >= mFirstCount)
            {
                return Concat(mFirst, mSecond.TakeN(mFirstCount));
            }
            else
            {
                return mFirst.TakeN(n);
            }
        }

        public override FList TakeWhile(FilterFxn f)
        {
            FList tmp = mFirst.TakeWhile(f);
            if (tmp.Count() == mFirstCount)
            {
                return Concat(mFirst, mSecond.TakeWhile(f));
            }
            else
            {
                return tmp;
            }

        }

        public override FList DropWhile(FilterFxn f)
        {
            FList tmp = mFirst.DropWhile(f);
            if (tmp.IsEmpty())
            {
                return mSecond.DropWhile(f);
            }
            else
            {
                return Concat(tmp, mSecond);
            }
        }
        #endregion
    }

    public class FArray<T> : FList
    {
        #region fields
        T[] m; 
        #endregion

        #region constructors
        public FArray(FList list, int n)
        {
            m = new T[n];
            int i = 0;
            list.ForEach(delegate(Object x) { if (i < n) m[i++] = (T)x; });
        }

        public FArray(FList list)
            : this(list, list.Count())
        {
        }

        public FArray(T[] x)
        {
            m = x;
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
            throw new Exception("CArray used CRangedArray as an iterator, therefore GotoNext() can never be called on it.");
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
            return TakeRange(n, Count() - n);
        }

        public override FList TakeN(int n)
        {
            return TakeRange(0, n);
        }

        public override FList TakeRange(int first, int count)
        {
            if ((first == 0) && (count == Count())) 
                return this;

            if (first + count > Count())
                count = Count() - first;

            switch (count)
            {
                case (0):
                    return Nil();
                case (1):
                    return MakeUnit(m[0]);
                case (2):
                    return MakePair(m[0], m[1]);
                default:
                    return new RangedArray<T>(m, first, count);
            }
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

    public class RangedArray<T> : FList
    {
        #region fields
        T[] m;
        int mFirst;
        int mCount;  
        #endregion

        #region cosntructors
        public RangedArray(T[] a, int first, int count)
        {
            if (count < 0)
                throw new Exception("Invalid count");
            if (first < 0)
                throw new Exception("Invalid range, first index must be non-negative");
            if (first + count > a.Length)
                count = a.Length - first;
            m = a;
            mFirst = first;
            mCount = count;
        } 
        #endregion

        #region abstract function overrides
        public override void ForEach(Accessor a)
        {
            for (int i = mFirst; i < mFirst + mCount; ++i)
            {
                a(m[i]);
            }
        }

        public override FList GetIter()
        {
            return new RangedArray<T>(m, mFirst, mCount);
        }

        public override FList GotoNext()
        {
            mFirst += 1; 
            mCount -= 1;
            return this;
        }

        public override bool IsEmpty()
        {
            return mCount == 0;
        }

        public override Object GetHead()
        {
            return m[mFirst];
        }
        #endregion 

        #region virtual function overrides
        public override int Count()
        {
            return mCount;
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

        public override FList DropWhile(FilterFxn f)
        {
            int n = CountWhile(f);
            return TakeRange(n, mCount - n);
        }

        public override object Nth(int n)
        {
            return m[n + mFirst];
        }

        public override Object Last()
        {
            return m[mFirst + mCount - 1];
        }

        public override FList DropN(int n)
        {
            return TakeRange(n, Count() - n);
        }

        public override FList TakeN(int n)
        {
            return TakeRange(0, n);
        }

        public override FList TakeRange(int first, int count)
        {
            if (first + count > mCount)
                count = mCount - first;

            switch (count)
            {
                case (0):
                    return Nil();
                case (1):
                    return MakeUnit(m[first + mFirst]);
                case (2):
                    return MakePair(m[first + mFirst], m[first + mFirst + 1]);
                default:
                    return new RangedArray<T>(m, mFirst, count);
            }
        }
        #endregion 
    }

    public class MappedFList : FList
    {
        #region fields
        MapFxn mMap;
        FList mList; 
        #endregion

        #region constructors
        public MappedFList(FList list, MapFxn map)
        {
            mList = list;
            mMap = map;
        } 
        #endregion

        #region abstract function overrides
        public override void ForEach(Accessor a)
        {
            mList.ForEach(delegate(object x) { a(mMap(x)); });
        }

        public override FList GetIter()
        {
            return new MappedFList(mList.GetIter(), mMap);
        }

        public override FList GotoNext()
        {
            mList = mList.GotoNext();
            return this;
        }

        public override bool IsEmpty()
        {
            return mList.IsEmpty();
        }

        public override object GetHead()
        {
            return mMap(mList.GetHead());
        }
        #endregion 

        #region virtual function overrides
        public override bool IsKnownFinite()
        {
            return mList.IsKnownFinite();
        }

        public override int Count()
        {
            return mList.Count();
        }

        public override object Last()
        {
            return mMap(mList.Last());
        }

        public override object Nth(int n)
        {
            return mMap(mList.Nth(n));
        }

        public override FList DropN(int n)
        {
            return new MappedFList(mList.DropN(n), mMap);
        }

        public override FList TakeN(int n)
        {
            return new MappedFList(mList.TakeN(n), mMap);
        }

        public override FList TakeRange(int first, int count)
        {
            return new MappedFList(mList.TakeRange(first, count), mMap);
        }
        #endregion 
    }

    public class FilteredFList : FList
    {
        #region fields
		FilterFxn mFilter;
        FList mList; 
	    #endregion

        #region constructors
        public FilteredFList(FList list, FilterFxn filter)
        {
            // this way we are guaranteed that either mList has a first value 
            // satisfying the filter, or is empty. 
            mList = list.DropWhile(NegateFilter(filter));
            mFilter = filter;
        }        
        #endregion

        #region abstract function overrides
        public override void ForEach(Accessor a)
        {
            mList.ForEach(delegate(object x) { if (mFilter(x)) a(x); });
        }

        public override FList GetIter()
        {
            return new FilteredFList(mList.GetIter(), mFilter);
        }

        public override FList GotoNext()
        {
            mList = mList.GotoNext();
            while (!mList.IsEmpty() && !mFilter(mList.GetHead()))
            {
                mList = mList.GotoNext();
            }
            if (mList.IsEmpty())
                return Nil();
            return this;
        }

        public override object GetHead()
        {
            return mList.GetHead();   
        }

        public override bool IsEmpty()
        {
            return mList.IsEmpty();
        }
        #endregion

        #region virtual function overrides
        public override int CountWhile(FilterFxn f)
        {
            return mList.CountWhile(ComposeFilters(f, mFilter));
        }

        public override bool IsKnownFinite()
        {
            return mList.IsKnownFinite();
        }
        #endregion
    }

    public class ConsCell : FList
    {
        #region fields
        object mHead;
        FList mTail; 
        #endregion

        #region constructors
        public ConsCell(object head, FList rest)
        {
            mHead = head;
            mTail = rest;
        } 
        #endregion

        #region abstract function overrides
        public override void ForEach(Accessor a)
        {
            a(mHead);
            mTail.ForEach(a);
        }

        public override FList GetIter()
        {
            return new ConsCell(mHead, mTail);
        }

        public override FList GotoNext()
        {
            if (mTail.IsEmpty())
            {
                return Nil();
            }

            mHead = mTail.GetHead();
            mTail = mTail.Tail();
            return this;
        }

        public override bool IsEmpty()
        {
            return false;
        }

        public override object GetHead()
        {
            return mHead;
        }
        #endregion

        #region virtual function overrides
        public override bool IsKnownFinite()
        {
            return false;
        }

        public override bool IsKnownInfinite()
        {
            return false;
        }

        public override int Count()
        {
            return 1 + mTail.Count();
        }

        public override int CountWhile(FilterFxn f)
        {
            if (!f(mHead)) return 0;
            return 1 + mTail.CountWhile(f);
        }

        public override object Nth(int n)
        {
            if (n == 0) 
                return mHead;
            else
                return mTail.Nth(n - 1);
        }

        public override FList DropN(int n)
        {
            if (n == 0)
                return this;
            else
                return mTail.DropN(n - 1);
        }
        #endregion
    }

    public class FListRepeater : FList
    {
        #region fields
        object mObject; 
        #endregion

        #region constructors
        public FListRepeater(Object o)
        {
            mObject = o;
        } 
        #endregion

        #region abstract function overrides
        public override void ForEach(Accessor a)
        {
            // Breaks only when a throws an exception
            while (true)
            {
                a(mObject);
            }
        }

        public override FList GetIter()
        {
            return this;
        }

        public override FList GotoNext()
        {
            return this;
        }

        public override bool IsEmpty()
        {
            return false;
        }

        public override Object GetHead()
        {
            return mObject;
        }
        #endregion 

        #region virtual override functions
        public override bool IsKnownFinite()
        {
            return false;
        }

        public override bool IsKnownInfinite()
        {
            return true;
        }

        public override object Nth(int n)
        {
            return mObject;
        }

        public override FList Tail()
        {
            return this;
        }

        public override Object Last()
        {
            return mObject;
        }

        public override FList Filter(FilterFxn f)        
        {
            if (f(mObject))
                return this;
            else 
                return Nil();
        }

        public override int Count()
        {
            throw new Exception("infinite list");
        }

        public override int CountWhile(FilterFxn f)
        {
            if (!f(mObject)) return 0;
            throw new Exception("inifinite");
        }


        public override object Fold(object init, FoldFxn f)
        {
            if (f(init, mObject) != init) throw new Exception("diverges");
            else return init;
        }

        public override FList Map(MapFxn f)
        {
            return new FListRepeater(f(mObject));
        }

        public override FList DropN(int n)
        {
            return this;
        }

        public override FList TakeN(int n)
        {
            return RangeGen(delegate(int i) { return mObject; }, 0, n);
        }

        public override FList TakeWhile(FilterFxn f)
        {
            return Gen(mObject, delegate(Object x) { return mObject; }, f);
        }

        public override FList DropWhile(FilterFxn f)
        {
            return this;
        }

        #endregion
    }

    public class RangeGenerator : FList
    {
        #region fields
        RangeGenFxn mFxn;
        int mFirst;
        int mCount; 
        #endregion

        #region constructors
        public RangeGenerator(RangeGenFxn f, int first, int count)
        {
            mFxn = f;
            mFirst = first;
            mCount = count;
        } 
        #endregion

        #region abstract function overrides
        public override void ForEach(Accessor a)
        {
            for (int i = mFirst; i < mFirst + mCount; ++i)
            {
                a(mFxn(i));
            }
        }

        public override FList GetIter()
        {
            return new RangeGenerator(mFxn, mFirst, mCount);
        }

        public override FList GotoNext()
        {
            ++mFirst;
            --mCount;
            return this;
        }

        public override bool IsEmpty()
        {
            return mCount == 0;
        }

        public override Object GetHead()
        {
            return mFxn(mFirst);
        }
        #endregion 

        #region virtual function overrides
        public override bool IsKnownFinite()
        {
            return true;
        }

        public override bool IsKnownInfinite()
        {
            return false;
        }

        public override int Count()
        {
            return mCount;
        }

        public override object Nth(int n)
        {
            return mFxn(n + mFirst);
        }

        public override FList Tail()
        {
            return DropN(1);
        }

        public override Object Last()
        {
            return mFxn(mFirst + mCount - 1);
        }

        public override FList DropN(int n)
        {
            return TakeRange(n, mCount - n);
        }

        public override FList TakeN(int n)
        {
            return TakeRange(0, n);
        }

        public override FList TakeRange(int first, int count)
        {
            if (count + first > mCount)
                count = mCount - first;

            switch (count)
            {
                case (0):
                    return Nil();
                case (1):
                    return MakeUnit(mFxn(mFirst + first));
                case (2):
                    return MakePair(mFxn(mFirst + first), mFxn(mFirst + first + 1));
                default:
                    return new RangeGenerator(mFxn, mFirst + first, count);
            }
        }

        public override int CountWhile(FilterFxn f)
        {
            for (int i = 0; i < mCount; ++i)
            {
                if (!f(mFxn(i + mFirst)))
                    return i;
            }
            return mCount;
        }

        public override FList DropWhile(FilterFxn f)
        {
            int n = CountWhile(f);
            return TakeRange(n, mCount - n);
        }
        #endregion
    }

    public class Generator : FList
    {
        #region fields
        object mFirst;
        MapFxn mNext;
        FilterFxn mCond; 
        #endregion

        #region constructors
        public Generator(object first, MapFxn next, FilterFxn cond)
        {
            mFirst = first;
            mNext = next;
            mCond = cond;
        }

        #endregion

        #region abstract function overrides
        public override void ForEach(Accessor a)
        {
            object cur = mFirst;
            while (mCond(cur))
            {
                a(cur);
                cur = mNext(cur);
            }
        }

        public override FList GetIter()
        {
            return new Generator(mFirst, mNext, mCond);
        }

        public override object GetHead()
        {
            return mFirst;
        }

        public override bool IsEmpty()
        {
            return !mCond(mFirst);
        }

        public override FList GotoNext()
        {
            mFirst = mNext(mFirst);
            return this;
        }
        #endregion 

        #region virtual function overrides
        public override bool IsKnownFinite()
        {
            return false;
        }

        public override bool IsKnownInfinite()
        {
            return false;
        }

        public override FList Tail()
        {
            return Gen(mNext(mFirst), mNext, mCond);
        }

        public override FList TakeWhile(FilterFxn f)
        {
            if (!mCond(mFirst) || !f(mFirst)) return Nil();
            return Gen(mFirst, mNext, ComposeFilters(f, mCond));
        }

        public override int CountWhile(FilterFxn f)
        {
            int n = 0;
            object cur = mFirst;
            while (mCond(cur))
            {
                ++n;
                cur = mNext(cur);
            }
            return n;
        }

        public override FList DropWhile(FilterFxn f)
        {
            object cur = mFirst;
            while (mCond(cur) && f(cur))
            {
                cur = mNext(cur);
            }
            if (mCond(cur)) return Nil();
            return Gen(cur, mNext, mCond);
        }
        #endregion 
    }


    public class FlattenedFList : FList
    {
        #region fields
        FList mList;
        #endregion

        #region constructor
        public FlattenedFList(FList list)
        {
            mList = list;
        }
        #endregion 

        #region abstract function overrides
        public override void ForEach(Accessor a)
        {
            mList.ForEach(delegate(object o) { (o as FList).ForEach(a); });
        }

        public override FList GetIter()
        {
            return new FlattenedFList(mList.GetIter());
        }

        public override FList GotoNext()
        {
            mList = mList.GotoNext();
            return this;
        }

        public override object GetHead()
        {
            return (mList.GetHead() as FList).GetHead();
        }

        public override bool IsEmpty()
        {
            return (!mList.IsEmpty()) && ((mList.GetHead() as FList).IsEmpty());
        }
        #endregion
    }
}
