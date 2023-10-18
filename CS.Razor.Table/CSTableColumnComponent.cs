using CS.Razor.Table.Expressions;
using CS.Razor.Table.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace CS.Razor.Table
{
    public partial class CSTableColumnComponent<TItem, TValue> : ComponentBase, ICSTableColumn
    {
        [Inject]
        IJSRuntime JSRuntime { get; set; }

        [CascadingParameter]
        public CSTableRow<TItem> TableRow { get; set; }

        [Parameter]
        public RenderFragment<TItem> ChildContent { get; set; }

        [Parameter]
        public Expression<Func<TItem, TValue>> Value { get; set; }

        [Parameter]
        public string Header { get; set; }

        [Parameter]
        public string Format { get; set; }

        [Parameter]
        public string Align { get; set; }

        [Parameter]
        public string Width { get; set; }

        [Parameter]
        public bool IsFilterable { get; set; } = true;
        protected bool AscendingOrder { get; set; }
        public bool HasChild => ChildContent != null;
        public bool IsHeader => TableRow.IsHeader;
        public bool IsFilter => TableRow.IsFilter;
        public string align => string.IsNullOrEmpty(Align) ? "" : $"text-align: {Align};";
        public string width => string.IsNullOrEmpty(Width) ? "" : $"width: {Width};";
        public object ColumnValue => GetColumnValue(Value);
        public string ColumnHeader => Header ?? (string)GetColumnHeader(Value.Body);
        public string HeaderProperty => (string)GetColumnHeader(Value.Body);
        protected string Operator { get; set; }

        protected Filter filter = new Filter() { FilterOperator = FilterOperator.Default };

        protected Dictionary<FilterOperator, string> dropdownOperators = new Dictionary<FilterOperator, string>();

        protected bool IsFilterDisabled => dropdownOperators.Count == 0 || !IsFilterable;
        protected bool IsSortingDisabled { get; set; }
        protected override void OnInitialized()
        {
            TableRow.Table.AddColumn(this);

            if (IsHeader)
            {
                TableRow.Table.AddHeader(ColumnHeader);
            }

            SetDropdownOperators();
            SetOperatorIcon(FilterOperator.Default);
        }

        public async Task SortAsync()
        {

            if(IsSortingDisabled) return;

            if (AscendingOrder)
            {
                TableRow.Table.SetSorterExpression(v => v.OrderBy(Value.Compile()));
            }
            else
            {
                TableRow.Table.SetSorterExpression(v => v.OrderByDescending(Value.Compile()));
            }

            await AddClasses();
            AscendingOrder = !AscendingOrder;
        }
        protected void SetFilterOperator(FilterOperator filterOperator)
        {
            filter.FilterOperator = filterOperator;
            SetOperatorIcon(filterOperator);
            FilterColum();
        }
        protected void FilterColum()
        {
            var propertyName = (string)GetColumnHeader(Value.Body);

            if (!string.IsNullOrEmpty(filter.SearchTerm))
            {
                filter.PropertyName = propertyName;

                Expression<Func<TItem, bool>> expression = ExpressionBuilder.GetFilterExpression<TItem>(filter);

                if (expression != null)
                {
                    var kvp = new KeyValuePair<string, Expression<Func<TItem, bool>>>(propertyName, expression);
                    TableRow.Table.RemoveAddFilterExpression(kvp);
                }
            }
            else
            {
                TableRow.Table.RemoveFilterExpression(propertyName);
            }
        }

        private void SetDropdownOperators()
        {
            var propertyName = (string)GetColumnHeader(Value.Body);
            Type type = typeof(TItem).GetProperty(propertyName).PropertyType;

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                type = Nullable.GetUnderlyingType(type);
            };

            switch (Type.GetTypeCode(type))
            {
                case TypeCode.String:
                    dropdownOperators.Add(FilterOperator.EqualTo, "= Equals");
                    dropdownOperators.Add(FilterOperator.NotEqualTo, "≠ Does not equal (Sensetive)");
                    dropdownOperators.Add(FilterOperator.Contains, "Contains");
                    dropdownOperators.Add(FilterOperator.IndexOf, "Contains (Index of)");
                    dropdownOperators.Add(FilterOperator.StartsWith, "Starts with");
                    dropdownOperators.Add(FilterOperator.EndsWith, "Ends with");
                    break;
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Single:
                    dropdownOperators.Add(FilterOperator.EqualTo, "= Equals");
                    dropdownOperators.Add(FilterOperator.NotEqualTo, "≠ Does not equal");
                    dropdownOperators.Add(FilterOperator.GreaterThan, "> Greater than");
                    dropdownOperators.Add(FilterOperator.GreaterThanOrEqualTo, "≥ Greater than or equal");
                    dropdownOperators.Add(FilterOperator.LessThan, "< Less than");
                    dropdownOperators.Add(FilterOperator.LessThanOrEqualTo, "≤ Less than or equal");
                    dropdownOperators.Add(FilterOperator.StartsWith, "Starts with");
                    dropdownOperators.Add(FilterOperator.Contains, "Contains");
                    break;
                case TypeCode.DateTime:
                    dropdownOperators.Add(FilterOperator.EqualTo, "= Equals");
                    dropdownOperators.Add(FilterOperator.NotEqualTo, "≠ Does not equal");
                    dropdownOperators.Add(FilterOperator.GreaterThan, "> Greater than");
                    dropdownOperators.Add(FilterOperator.GreaterThanOrEqualTo, "≥ Greater than or equal");
                    dropdownOperators.Add(FilterOperator.LessThan, "< Less than");
                    dropdownOperators.Add(FilterOperator.LessThanOrEqualTo, "≤ Less than or equal");
                    break;
                case TypeCode.Boolean:
                    dropdownOperators.Add(FilterOperator.EqualTo, "= Equals");
                    dropdownOperators.Add(FilterOperator.NotEqualTo, "≠ Does not equal");
                    break;
                default:
                    dropdownOperators = new Dictionary<FilterOperator, string>();
                    IsSortingDisabled = true; 
                    break;
            }
        }
        private void SetOperatorIcon(FilterOperator filterOperator)
        {
            switch (filterOperator)
            {
                case FilterOperator.EqualTo:
                    Operator = "=";
                    break;
                case FilterOperator.NotEqualTo:
                    Operator = "≠";
                    break;
                case FilterOperator.GreaterThan:
                    Operator = ">";
                    break;
                case FilterOperator.GreaterThanOrEqualTo:
                    Operator = "≥";
                    break;
                case FilterOperator.LessThan:
                    Operator = "<";
                    break;
                case FilterOperator.LessThanOrEqualTo:
                    Operator = "≤";
                    break;
                case FilterOperator.Contains:
                    Operator = "Contains";
                    break;
                case FilterOperator.StartsWith:
                    Operator = "Starts";
                    break;
                case FilterOperator.EndsWith:
                    Operator = "Ends";
                    break;
                case FilterOperator.IndexOf:
                    Operator = "Index";
                    break;
                default:
                    Operator = "";
                    break;
            }
        }
        private object GetColumnValue(Expression<Func<TItem, TValue>> expression)
        {
            var compiledExpression = expression.Compile();
            try
            {
                var value = compiledExpression(TableRow.RowData);
                return string.IsNullOrEmpty(Format) ? value : string.Format("{0:" + Format + "}", value);
            }
            catch
            {
                return null;
            }

        }
        private object GetColumnHeader(Expression expression)
        {
            try
            {
                switch (expression.NodeType)
                {
                    case ExpressionType.MemberAccess:
                        return ((MemberExpression)expression).Member.Name;
                    case ExpressionType.Convert:
                        return GetColumnHeader(((UnaryExpression)expression).Operand);
                    default:
                        throw new NotSupportedException(expression.NodeType.ToString());
                }
            }
            catch
            {
                return null;
            }
        }
        private async Task AddClasses()
        {
            string classes;

            if (AscendingOrder)
            {
                classes = "bi-arrow-up";
            }
            else
            {
                classes = "bi-arrow-down";

            }

            await JSRuntime.InvokeVoidAsync("applyHeaderStyle", ColumnHeader, classes);



            foreach (var header in TableRow.Headers)
            {
                if (header != ColumnHeader)
                {
                    await JSRuntime.InvokeVoidAsync("removeHeaderStyle", header);
                }
            }
        }

    }
}
