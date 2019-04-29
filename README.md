# EnumValidatorSharp
Simple program which iterates enumerations in C# assembly and verifies that enumeration members don't contain duplicates or other inconsistencies.

## Motivation

In many larger .NET projects there exists many enums, some very large that may be numbered explicitly.
C# allows the integer values for enum to be duplicates so no error or even a warning is shown. This might however be unwanted and there should not be any duplicate values in enums.

This program uses reflection to load external assembly, iterate the enums and check that there are no duplicates.

## How to use

* Clone the repo
* Build the program
* Drag and drop the external DLL or EXE on EnumValidator.exe
* Check the output for warnings and listings of duplicate values