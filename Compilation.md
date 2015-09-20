# CIL Compiler #

The Cat interpreter comes with a built-in [Common Intermediate Language](CIL.md) (CIL) compiler. The compiler is activated using the `#c` [meta-command](MetaCommand.md) converts a [quotation](Quotation.md) into a dynamic assembly, and saves it as to the file "out.exe".

A dynamic assembly can be run using the `#run` [meta-command](MetaCommand.md).