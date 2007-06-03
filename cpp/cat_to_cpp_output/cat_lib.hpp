// Public domain Cat interpreter 
// by Christopher Diggins
// http://www.cat-language.com

#include <algorithm>
#include <assert.h>

#include "..\ootl\ootl_object.hpp"
#include "..\ootl\ootl_stack.hpp"
#include "..\ootl\ootl_timer.hpp"

using namespace ootl;

//////////////////////////////////////////////////////////////////////////////
// global data

stack<object> stk;

//////////////////////////////////////////////////////////////////////////////
// typedefs 

typedef void(*fxn_ptr)();
typedef stack<object> list;

//////////////////////////////////////////////////////////////////////////////
// forward declarations

void _eval(object& o);

//////////////////////////////////////////////////////////////////////////////
// debugging stuff

//#define VERBOSE

#ifdef VERBOSE
#define call(FXN) printf("calling %s\n", #FXN); FXN(); print_stack(); /* */
#else 
#define call(FXN) FXN(); /* */
#endif

void cat_assert(bool b)
{
	if (!b)
		throw std::exception("failed assertion");
}

//////////////////////////////////////////////////////////////////////////////
// function types

struct prim_function
{
	prim_function(const fxn_ptr& f)
		: fxn(f)
	{ }
	prim_function(const prim_function& f)
		: fxn(f.fxn)
	{ }
	bool operator==(const prim_function& x) const 
	{
		return fxn == x.fxn;
	}
	fxn_ptr fxn;
};

struct quoted_value
{ 
	quoted_value(object& o)
	{
		invalid = false;
		o.move_to(value);
	}
	quoted_value(const quoted_value& x)
		: value(x.value)
	{
		invalid = false;
	}
	bool operator==(const quoted_value& x) const 
	{
		return value == x.value;
	}
	// can only be called once. This is critical 
	// for fast "quote apply" instructions. Consider "1000000 n quote" ... "apply"
	void eval()
	{
		cat_assert(!invalid);
		stk.push_nocreate();
		value.move_to(stk.top());
		invalid = true;
	}
	bool invalid;
	object value;
};

struct composed_function
{
	composed_function(const composed_function& cf)
		: fxns(cf.fxns)
	{ 
		invalid = false;
	}
	composed_function(object& first, object& second)
	{
		invalid = false;
		fxns.push_nocreate();
		first.move_to(fxns.top());
		fxns.push_nocreate();
		second.move_to(fxns.top());
	}
	void compose_with(object& o)
	{
		// TODO: check that o is a function. 
		fxns.push_nocreate();
		o.move_to(fxns.top());
	}
	// can only be called once 
	void eval()
	{
		cat_assert(!invalid);
		fxns.foreach(_eval);
		invalid = true;
	}
	bool operator==(const composed_function& x) const 
	{
		return fxns == x.fxns;
	}
	bool invalid;
	list fxns;
};

//////////////////////////////////////////////////////////////////////////////
// stack display functions

void print_object(object& o);

void print_list(list& l)
{
	printf("(");
	l.foreach(print_object);
	printf(") ");
}

void print_object(object& o)
{
	if (o.is<int>())
	{			
		printf("%d ", o.to<int>());
	}
	else if (o.is<bool>())
	{			
		if (o.to<bool>())
			printf("true ");
		else 
			printf("false ");
	}
	else if (o.is<list>())
	{
		print_list(o.to<list>());
	}
	else if (o.is<quoted_value>())
	{
		printf("[");
		print_object(o.to<quoted_value>().value);
		printf("] ");
	}
	else if (o.is<composed_function>())
	{
		printf("{");
		print_list(o.to<composed_function>().fxns);
		printf("} ");
	}
	else if (o.is<prim_function>())
	{
		printf("fxn ");
	}
	else if (o.is_empty())
	{
		printf("invalid object!");
		cat_assert(false);
	}
	else
	{
		cat_assert(false);
	}
}

void print_stack()
{		
	// For debugging:
	stk.foreach(print_object);
	puts("");
}

//////////////////////////////////////////////////////////////////////////////
// Implementation functions

// note: a function object can only ever be evaluated once.	
// this is because a quoted_value will literally move its value into 
// the stack invalidating itself
void _eval(object& o)
{
	if (o.is<quoted_value>())
	{
		o.to<quoted_value>().eval();
	}
	else if (o.is<composed_function>())
	{
		o.to<composed_function>().eval();
	}
	else if (o.is<prim_function>())
	{
		o.to<prim_function>().fxn();
	}
	else
	{
		// Not a function. Note that you could simply do nothing thus 
		// echoing the value on the stack. 
		// This would give you semantics like Joy or Scheme.
		cat_assert(false);
	}
	o.release_nodestroy();
}

// note: this is not a reference, so the object doesn't get invalidated
void _eval_copy(object o)
{
	_eval(o);
}

void push_function(fxn_ptr fp)
{
	stk.push(prim_function(fp));
#ifdef VERBOSE
	print_stack();
#endif
}

template<typename T>
void push_literal(const T& x)
{
	stk.push(x);
#ifdef VERBOSE
	print_stack();
#endif
}

//////////////////////////////////////////////////////////////////////////////
// primitive functions 

// Could be bootstrapped, but it would be very slow and complicated. It would require
// using the Y or M combinator, and would be of only mild theoretical interested
void _while()
{
	object cond;
	object body;
	stk.top().move_to(cond);
	stk.pop_nodestroy();
	stk.top().move_to(body);
	stk.pop_nodestroy();
	
	// force a copy of the function: otherwise it would get invalidated
	_eval_copy(cond);
	while (stk.pull().to<bool>())
	{
		_eval_copy(body);
		_eval_copy(cond);
	}
}

// Could also be bootstrapped, but would be ridiculously slow
void _empty()
{
	stk.push(stk.top().to<list>().is_empty());
}

void _add__int()
{
	cat_assert(stk.count() >= 2);
	int n = stk.pull().to<int>();
	int m = stk.pull().to<int>();
	stk.push(m + n);
}

void _mul__int()
{
	cat_assert(stk.count() >= 2);
	int n = stk.pull().to<int>();
	int m = stk.pull().to<int>();
	stk.push(m * n);
}

void _div__int()
{
	cat_assert(stk.count() >= 2);
	int n = stk.pull().to<int>();
	int m = stk.pull().to<int>();
	stk.push(m / n);
}

void _mod__int()
{
	cat_assert(stk.count() >= 2);
	int n = stk.pull().to<int>();
	int m = stk.pull().to<int>();
	stk.push(m % n);
}

void _lt__int()
{
	cat_assert(stk.count() >= 2);
	int n = stk.pull().to<int>();
	int m = stk.pull().to<int>();
	stk.push(m < n);
}

void _neg__int()
{
	cat_assert(stk.count() >= 1);
	int n = stk.pull().to<int>();
	stk.push(-n);
}

void _halt()
{
	stk.pop();
	perror("test failed");
	//exit(1);
}

void _nil()
{
	stk.push(list());
}

void _cons()
{
	cat_assert(stk.count() >= 2);
	object o;
	stk.top().move_to(o);
	stk.pop_nodestroy();
	list& lst = stk.top().to<list>();
	lst.push_nocreate();
	o.move_to(lst.top());
}

void _uncons()
{
	cat_assert(stk.count() >= 1);
	list& lst = stk.top().to<list>();
	if (lst.is_empty())
	{
		_nil();
		return;
	}
	stk.push_nocreate();
	lst.top().move_to(stk.top());		
	lst.pop_nodestroy();
}

void _eq()
{
	cat_assert(stk.count() >= 2);
	object o;
	stk.top().move_to(o);
	stk.pop_nodestroy();
	if (stk.top() == o)
		stk.top() = true;
	else
		stk.top() = false;
}

void _dup()
{
	cat_assert(stk.count() >= 1);
	stk.push(stk.top());
}

void _pop()
{
	cat_assert(stk.count() >= 1);
	stk.pop();
}

void _true()
{
	stk.push(true);
}

void _false()
{
	stk.push(false);
}

void _swap()
{
	cat_assert(stk.count() >= 2);
	object& first = stk.top();
	object& second = stk[1];
	object tmp;
	first.move_to(tmp);
	second.move_to(first);
	tmp.move_to(second);
}

void _quote()
{
	cat_assert(stk.count() >= 1);
	object o;
	stk.top().move_to(o);
	stk.pop_nodestroy();
	stk.push(quoted_value(o));
}

void _if()
{
	cat_assert(stk.count() >= 3);
	object onfalse;
	stk.top().move_to(onfalse);
	stk.pop_nodestroy();
	object ontrue;
	stk.top().move_to(ontrue);
	stk.pop_nodestroy();
	bool bCond = stk.top().to<bool>();
	stk.pop_nodestroy();
	if (bCond)
		_eval(ontrue);
	else 
		_eval(onfalse);
}

void _compose()
{
	cat_assert(stk.count() >= 2);
	object o;
	stk.top().move_to(o);
	stk.pop_nodestroy();
	if (stk.top().is<composed_function>())
	{
		composed_function& cf = stk.top().to<composed_function>();
		cf.compose_with(o);
	}
	else
	{
		object o2;
		stk.top().move_to(o2);
		stk.pop_nodestroy();
		stk.push(composed_function(o2, o));
	}
}

void _test()
{
	static int nTest = 0;
	printf("test %d\n", nTest++);
	scoped_timer timer;
	
	cat_assert(stk.count() == 1);
	_eval(stk.pull());
	if (stk.count() != 1)
	{
		perror("test failed: expected a single value after running test");
	}
	else if (!stk.top().is<bool>())
	{
		perror("test failed: expected a boolean value after running test");
	}
	if (!stk.top().to<bool>())
	{
		perror("test failed: result was false");
	}
	stk.clear();
	return;
}