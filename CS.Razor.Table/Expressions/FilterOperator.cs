using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CS.Razor.Table.Expressions
{
    public enum FilterOperator
    {
        EqualTo,
        NotEqualTo,
        Contains,
        IndexOf,
        StartsWith,
        EndsWith,
        LessThan,
        LessThanOrEqualTo,
        GreaterThan,
        GreaterThanOrEqualTo,
        Default
    }
}
