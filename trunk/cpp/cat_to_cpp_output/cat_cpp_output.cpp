// cat_cpp_output.cpp : Defines the entry point for the console application.
//

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
}

int main(int argc, char* argv[])
{
	//unit_tests();
	try
	{
		_run__tests();
	}
	catch (object::bad_object_cast e)
	{
		printf("type error casting from %s to %s\n", e.from.name(), e.to.name());
	}
	return 0;
}

