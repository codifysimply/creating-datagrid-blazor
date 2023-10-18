using CS.Razor.Table.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CS.Razor.Table
{
    public class CSTableRow<TItem>
    {
        public ICSTable<TItem> Table { get; set; }
        public bool IsHeader { get; set; }
        public bool IsFilter { get; set; }
        public int RowIndex { get; set; }
        public TItem RowData { get; set; }
        public List<string> Headers { get; set; }
    }
}
