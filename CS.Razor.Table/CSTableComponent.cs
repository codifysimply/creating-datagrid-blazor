using CS.Razor.Table.Interfaces;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;

namespace CS.Razor.Table
{
    public partial class CSTableComponent<TItem> : ComponentBase, ICSTable<TItem>
    {
        [Parameter]
        public RenderFragment ChildContent { get; set; }

        [Parameter]
        public IEnumerable<TItem> Items { get; set; }

        [Parameter]
        public string Width { get; set; } = "100%";

        [Parameter]
        public bool ShowFiletr { get; set; }

        protected readonly List<ICSTableColumn> columns = new List<ICSTableColumn>();

        private Expression<Func<IEnumerable<TItem>, IOrderedEnumerable<TItem>>> sortExpression;

        private Dictionary<string, Expression<Func<TItem, bool>>> filters = new Dictionary<string, Expression<Func<TItem, bool>>>();

        protected IEnumerable<TItem> filteredItems = new List<TItem>();

        protected List<string> headers;


        protected override void OnInitialized()
        {
            filteredItems = Items.ToList();
        }

        protected override void OnAfterRender(bool firstRender)
        {

            if (firstRender)
            {
                StateHasChanged();
            }
        }

        public void AddColumn(ICSTableColumn column)
        {
            columns.Add(column);
        }

        public void AddHeader(string header)
        {
            headers ??= new List<string>();
            headers.Add(header);
        }

        public void SetSorterExpression(Expression<Func<IEnumerable<TItem>, IOrderedEnumerable<TItem>>> expression)
        {
            sortExpression = expression;
            StateHasChanged();
        }

        public void RemoveAddFilterExpression(KeyValuePair<string, Expression<Func<TItem, bool>>> kvp)
        {
            filters.Remove(kvp.Key);

            filters.Add(kvp.Key, kvp.Value);

            StateHasChanged();
        }

        public void RemoveFilterExpression(string key)
        {
            var removed = filters.Remove(key);
            if (removed)
            {
                StateHasChanged();
            }
        }

        public IEnumerable<TItem> FilteredItems
        {
            get
            {

                if (filters.Count > 0)
                {
                    var list = filters.Select(kvp => kvp.Value.Compile());
                    filteredItems = Items.Where(v => list.All(filter => filter(v)));
                }
                else
                {
                    filteredItems = Items;
                }

                if (sortExpression != null)
                {
                    var compiledExpression = sortExpression.Compile();
                    filteredItems = compiledExpression(filteredItems);
                }
                return filteredItems;
            }
        }

    }
}
