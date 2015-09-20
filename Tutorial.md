# Introduction #

Cat is a [functional](Functional.md) [stack-based programming language](StackLanguages.md). All operations in Cat manipulate a single shared stack.

If you type a number in Cat it is pushed onto the stack as so:

```
>> 2
stack: 2
>> 3
stack: 2 3
>> 4
stack: 2 3 4
```

If you write an operator it will apply to the top two items

```
>> add
stack: 2 7
```

If you want to remove an item from the stack you can call `pop`:

```
>> pop 
stack: 2
```

You can also enter multiple numbers or instructions on the same line:

```
>> 4 4 add
stack: 2 8 
```

You can remove all items from a stack by calling `clr`:

```
>> clr
stack: _empty_
```

The three basics instructions for manipulating the stack are:

  * `pop` - remove top item from stack
  * `dup` - duplicate top item on stack
  * `swap` - swap the top two items on the stack

## More Resources ##

  * The [manual](http://www.cat-language.com/manual.html) has more in depth information about the language including a list of [primitives](http://www.cat-language.com/manual.html#primitives).
  * The wiki contains several [code examples](http://code.google.com/p/cat-language/w/list?q=label:CodeExample)
  * The [standard library](Library.md) contains definitions of many common functions.


