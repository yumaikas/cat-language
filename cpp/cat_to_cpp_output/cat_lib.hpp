// Public domain Cat level-0 interpreter 

#include <algorithm>
#include <assert.h>

#include "..\ootl\ootl_object.hpp"
#include "..\ootl\ootl_stack.hpp"

namespace cat
{
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

	void (object& o);

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
		fxn_ptr fxn;
	};

	struct quoted_value
	{ 
		quoted_value(object& o)
		{
			o.move_to(value);
		}
		quoted_value(const quoted_value& x)
			: value(x.value)
		{
		}
		// can only be called once. This is critical 
		// for fast "quote apply" instructions
		void eval()
		{
			stk.push_nocreate();
			value.move_to(stk.top());
		}
		object value;
	};

	struct composed_function
	{
		composed_function(const composed_function& cf)
			: fxns(cf.fxns)
		{ }
		composed_function(object& first, object& second)
		{
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
			fxns.foreach(_eval);
			// crucial, because it may contain quoted_values, which can only 
			// be called once 
			fxns.clear_nodestroy();
		}
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
		else if (o.is<list>())
		{
			print_list(o.to<list>());
		}
		else if (o.is<quoted_value>())
		{
			printf("[");
			print_object(o.to<quoted_value>().value);
			printf("]");
		}
		else if (o.is<composed_function>())
		{
			printf("{");
			print_list(o.to<composed_function>().fxns);
			printf("}");
		}
		else if (o.is<prim_function>())
		{
			printf("function");
		}
		else
		{
			assert(false);
		}
	}

	void print_stack()
	{		
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
			assert(false);
		}
		o.release_nodestroy();
	}
	
	void push_quotation(fxn_ptr fp)
	{
		stk.push(prim_function(fp));
	}

	template<typename T>
	void push_literal(const T& x)
	{
		stk.push(x);
	}

	//////////////////////////////////////////////////////////////////////////////
	// primitive functions 

	void _add_int()
	{
		int n = stk.pull().to<int>();
		int m = stk.pull().to<int>();
		stk.push(n + m);
	}

	void _nil()
	{
		stk.push(list());
	}

	void _cons()
	{
		object tmp;
		stk.top().move_to(tmp);
		stk.pop_nodestroy();
		list& target = stk.top().to<list>();
		target.push_nocreate();
		tmp.move_to(target.top());
	}

	void _dup()
	{
		stk.push(stk.top());
	}

	void _swap()
	{
		object& first = stk.top();
		object& second = stk[1];
		object tmp;
		first.move_to(tmp);
		second.move_to(first);
		tmp.move_to(second);
	}

	void _apply()
	{
		object o;
		stk.top().move_to(o);
		stk.pop_nodestroy();
		eval(o);
	}

	void _quote()
	{
		object o;
		stk.top().move_to(o);
		stk.pop_nodestroy();
		stk.push(quoted_value(o));
	}

	void _compose()
	{
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
			stk.push(composed_function(o, o2));
		}
	}
}
