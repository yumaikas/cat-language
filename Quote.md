# Quote #

The quote primitive function pops a value from the stack, and pushes a [nullary](Arity.md) function that always returns that value onto the stack.

The quote instruction is key to higher-order functional programming, and is used in the definition of basic [higher-order functions](HigherOrder.md) such as [curry](Currying.md).

## Type ##

```
quote : ('a -> ( -> 'a))
```