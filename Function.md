# Function #

A function is a named expression that [consumes](Consumption.md) zero or more values from the stack, and [produces](Production.md) zero or more values on to the stack. A function may also have [side-effects](SideEffect.md).

A function can be pushed on to a stack by [quoting it](Quotation.md).

## Examples ##

Some example of function definitions are:

```
>> define f { 40 2 + }
stack: _empty_
>> define g { 42 eq ["correct answer"] ["wrong answer"] if writeln }
stack: _empty_
>> 12 g
wrong answer
stack: _empty_
>> f g
that is the answer
stack: _empty_
>> [f]
stack: [f]
```