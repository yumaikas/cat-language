// Public domain Cat interpreter
// by Christopher Diggins
// http://www.cat-language.com

#include "cat_lib.hpp"

#include "output.hpp"

void unit_tests()
{
	cat_assert(stk.count() == 0);
	push_literal(42);
	cat_assert(stk.count() == 1);
	cat_assert(stk[0] == 42);
	call(_dup);
	cat_assert(stk.count() == 2);
	cat_assert(stk[1] == 42);
	call(_pop);
	cat_assert(stk.count() == 1);
	call(_inc);
	cat_assert(stk[0] == 43);
	push_function(_inc);
	cat_assert(stk.count() == 2);
	call(_apply);
	cat_assert(stk.count() == 1);
	cat_assert(stk[0] == 44);
	call(_dup);
	call(_eq);
	cat_assert(stk.count() == 1);
	cat_assert(stk[0] == true);
	call(_pop);
	push_literal(1);
	push_literal(2);
	call(_add__int);
	cat_assert(stk.count() == 1);
	push_literal(3);
	call(_eq);
	cat_assert(stk.count() == 1);
	cat_assert(stk[0] == true);
	call(_pop);
	
	// empty list comparisons
	call(_nil);
	call(_nil);
	call(_eq);
	cat_assert(stk[0] == true);
	call(_pop);

	// non-empty list comparison
	call(_nil);
	push_literal(1);
	call(_cons);
	call(_nil);
	push_literal(1);
	call(_cons);
	call(_eq);
	cat_assert(stk[0] == true);
	call(_pop);

	// composition tests
	push_literal(1);
	push_literal(2);
	push_literal(3);
	push_function(_mul__int);
	push_function(_add__int);
	call(_compose);
	call(_apply);
	cat_assert(stk[0] == 7);
	call(_pop);

	// while test
	push_literal(0);
	push_function(_inc);
	push_function(_dup);
	push_literal(3);
	call(_quote);
	call(_compose);
	push_function(_lteq__int);
	call(_compose);
	call(_while);
	cat_assert(stk[0] == 4);
	call(_pop);

	// whilene test
	push_literal(0);
	call(_nil);
	push_function(_inc);
	push_function(_dip);
	call(_curry);
	call(_whilene);
	cat_assert(stk[0] == 0);
	call(_pop);
}

/// Some custom stuff.
void _fib();

void _anon3000()
{
	call(_pop);
	push_literal(1);
}

void _anon3001()
{
	call(_dec);
	object tmp = stk.top();
	call(_fib);
	stk.push(tmp);
	call(_dec);
	call(_fib);
	call(_add__int);
}

void _fib()
{
	call(_dup);
	push_literal(1);
	call(_lteq__int);
	bool b = stk.pull().to<bool>();
	if (b)
		call(_anon3000) 
	else
		call(_anon3001);
}

void _fib_test()
{
	scoped_timer timer;
	push_literal(26); // 3.92, 3.29
	call(_fib);
}

int main(int argc, char* argv[])
{
	_fib_test();
	print_stack();

    //unit_tests();
	try
	{
		//_run__tests();
	}
	catch (object::bad_object_cast e)
	{
		printf("type error casting from %s to %s\n", e.from.name(), e.to.name());
	}
	printf("finished testing, no error messages is an excellent sign!\n");
	printf("press any key to continue ...\n");
	getchar();
	return 0;
}

