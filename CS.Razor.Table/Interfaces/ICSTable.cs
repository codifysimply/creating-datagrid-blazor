using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace CS.Razor.Table.Interfaces
{
    public interface ICSTable<TItem>
    {
        void AddColumn(ICSTableColumn column);
        void AddHeader(string header);
        void SetSorterExpression(Expression<Func<IEnumerable<TItem>, IOrderedEnumerable<TItem>>> expression);
        void RemoveAddFilterExpression(KeyValuePair<string, Expression<Func<TItem, bool>>> kvp);
        void RemoveFilterExpression(string key);
    }
}
