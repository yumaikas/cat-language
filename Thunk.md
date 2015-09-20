# Thunk #

A thunk is a [nullary](Arity.md) (i.e. zero-argument) function that returns a constant that is bound to the lexical scope in which it is created.

In Cat a thunk can be emulated using the [quote instruction](Quote.md):

```
define thunk : ('a -> ( -> 'a))
{ quote }
```