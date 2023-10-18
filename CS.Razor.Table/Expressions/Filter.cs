using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CS.Razor.Table.Expressions
{
    public class Filter
    {
        public FilterOperator FilterOperator { get; set; }
        public string PropertyName { get; set; }
        public string SearchTerm { get; set; }
    }
}
