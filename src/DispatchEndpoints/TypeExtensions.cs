using System;
using System.Collections.Generic;
using System.Text;

namespace DispatchEndpoints;

internal static class TypeExtensions
{
    public static bool IsInternal(this Type t)
        => t.IsNotPublic &&
           !t.IsVisible &&
           !t.IsPublic &&
           !t.IsNested &&
           !t.IsNestedPublic &&
           !t.IsNestedFamily &&
           !t.IsNestedPrivate &&
           !t.IsNestedAssembly &&
           !t.IsNestedFamORAssem &&
           !t.IsNestedFamANDAssem;
}