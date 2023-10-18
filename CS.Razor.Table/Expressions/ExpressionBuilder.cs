using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CS.Razor.Table.Expressions
{
    public class ExpressionBuilder
    {
        public static Expression<Func<TItem, bool>> GetFilterExpression<TItem>(Filter filter)
        {
            Type type = typeof(TItem).GetProperty(filter.PropertyName).PropertyType;

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                type = Nullable.GetUnderlyingType(type);
            };

            switch (Type.GetTypeCode(type))
            {
                case TypeCode.String:
                    return GetExpressionString<TItem>(filter);
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
                    return GetExpressionNumeric<TItem>(filter);
                case TypeCode.DateTime:
                    return GetExpressionDateTime<TItem>(filter);
                case TypeCode.Boolean:
                    return GetExpressionBool<TItem>(filter);
                default:
                    return null;
            }
        }
        private static Expression<Func<TItem, bool>> GetExpressionString<TItem>(Filter filter)
        {
            if (string.IsNullOrEmpty(filter.SearchTerm))
            {
                return null;
            }

            ParameterExpression parameter = Expression.Parameter(typeof(TItem), "item");

            Expression property = GetPropertyExpression(parameter, filter.PropertyName);

            MethodInfo Operator;

            switch (filter.FilterOperator)
            {
                case FilterOperator.EqualTo:
                    Operator = typeof(string).GetMethod("Equals", new Type[] { typeof(string) });
                    break;
                case FilterOperator.Contains:
                    Operator = typeof(string).GetMethod("Contains", new Type[] { typeof(string) });
                    break;
                case FilterOperator.StartsWith:
                    Operator = typeof(string).GetMethod("StartsWith", new Type[] { typeof(string) });
                    break;
                case FilterOperator.EndsWith:
                    Operator = typeof(string).GetMethod("EndsWith", new Type[] { typeof(string) });
                    break;
                case FilterOperator.IndexOf:
                    ConstantExpression constant = Expression.Constant(filter.SearchTerm, typeof(string));

                    MethodCallExpression indexOfmethod = Expression.Call(property,
                        "IndexOf",
                        null,
                        constant,
                        Expression.Constant(StringComparison.InvariantCultureIgnoreCase));

                    BinaryExpression indexOfBody = Expression.GreaterThanOrEqual(indexOfmethod, Expression.Constant(0));

                    // Type check // We don't need type check

                    // typeof(searchTerm) == typeof(string)
                    TypeBinaryExpression checkForType = Expression.TypeEqual(Expression.Constant(filter.SearchTerm), typeof(string));

                    ConditionalExpression checkForTypeCondition = Expression.Condition(checkForType, indexOfBody, Expression.Constant(false));

                    BinaryExpression checkForNull = Expression.Equal(property, Expression.Constant(null));

                    ConditionalExpression checkForNullCondition = Expression.Condition(checkForNull, Expression.Constant(false), indexOfBody);

                    BinaryExpression conditions = Expression.AndAlso(checkForNullCondition, checkForTypeCondition);

                    return Expression.Lambda<Func<TItem, bool>>(conditions, parameter);

                case FilterOperator.NotEqualTo:
                    Expression notEqualMethod = Expression.NotEqual(property, Expression.Constant(filter.SearchTerm));
                    return Expression.Lambda<Func<TItem, bool>>(notEqualMethod, parameter);
                default:
                    Operator = typeof(string).GetMethod("Contains", new Type[] { typeof(string) });
                    break;
            }

            MethodInfo trim = typeof(string).GetMethod("Trim", Type.EmptyTypes);

            MethodCallExpression trimMethod = Expression.Call(property, trim);

            MethodInfo toLower = typeof(string).GetMethod("ToLower", Type.EmptyTypes);

            MethodCallExpression toLowerMethod = Expression.Call(trimMethod, toLower);

            ConstantExpression trimConstant = Expression.Constant(filter.SearchTerm.ToLower(), typeof(string));

            MethodCallExpression body = Expression.Call(toLowerMethod, Operator, trimConstant);

            BinaryExpression nullCkeck = Expression.Equal(property, Expression.Constant(null));

            ConditionalExpression condition = Expression.Condition(nullCkeck, Expression.Constant(false), body);

            return Expression.Lambda<Func<TItem, bool>>(condition, parameter);
        }
        private static Expression<Func<TItem, bool>> GetExpressionNumeric<TItem>(Filter filter)
        {
            ParameterExpression parameter = Expression.Parameter(typeof(TItem), "item");
            Expression property = GetPropertyExpression(parameter, filter.PropertyName);
            bool nullableProperty = IsNullableProperty(property);
            object parsedSearchTerm = ParseNumeric(property.Type, filter.SearchTerm, nullableProperty, out Type nonNullableType);

            Expression comparison;

            switch (filter.FilterOperator)
            {
                case FilterOperator.EqualTo:
                    property = nullableProperty ? Expression.Convert(property, nonNullableType) : property;
                    comparison = Expression.Equal(property, Expression.Constant(parsedSearchTerm));
                    break;
                case FilterOperator.NotEqualTo:
                    property = nullableProperty ? Expression.Convert(property, nonNullableType) : property;
                    comparison = Expression.NotEqual(property, Expression.Constant(parsedSearchTerm));
                    break;
                case FilterOperator.GreaterThan:
                    property = nullableProperty ? Expression.Convert(property, nonNullableType) : property;
                    comparison = Expression.GreaterThan(property, Expression.Constant(parsedSearchTerm));
                    break;
                case FilterOperator.LessThan:
                    property = nullableProperty ? Expression.Convert(property, nonNullableType) : property;
                    comparison = Expression.LessThan(property, Expression.Constant(parsedSearchTerm));
                    break;
                case FilterOperator.GreaterThanOrEqualTo:
                    property = nullableProperty ? Expression.Convert(property, nonNullableType) : property;
                    comparison = Expression.GreaterThanOrEqual(property, Expression.Constant(parsedSearchTerm));
                    break;
                case FilterOperator.LessThanOrEqualTo:
                    property = nullableProperty ? Expression.Convert(property, nonNullableType) : property;
                    comparison = Expression.LessThanOrEqual(property, Expression.Constant(parsedSearchTerm));
                    break;
                case FilterOperator.Contains:
                case FilterOperator.StartsWith:
                    MethodInfo comparisonMethod = typeof(string).GetMethod(filter.FilterOperator == FilterOperator.Contains ? "Contains" : "StartsWith", new Type[] { typeof(string) });
                    MethodCallExpression convertMethod = Expression.Call(Expression.Convert(property, typeof(object)), typeof(object).GetMethod("ToString"));
                    ConstantExpression constant = Expression.Constant(filter.SearchTerm);
                    comparison = Expression.Call(convertMethod, comparisonMethod, constant);
                    break;
                default:
                    property = nullableProperty ? Expression.Convert(property, nonNullableType) : property;
                    comparison = Expression.Equal(property, Expression.Constant(parsedSearchTerm));
                    break;
            }


            Expression method = nullableProperty ?
                Expression.AndAlso(Expression.Equal(Expression.Property(property, "HasValue"), Expression.Constant(true)), comparison) :
                comparison;

            BinaryExpression nullCkeck = Expression.Equal(parameter, Expression.Constant(null));
            Expression condition = Expression.Condition(nullCkeck, Expression.Constant(false), method);
            return Expression.Lambda<Func<TItem, bool>>(condition, parameter);
        }
        private static Expression<Func<TItem, bool>> GetExpressionBool<TItem>(Filter filter)
        {
            ParameterExpression parameter = Expression.Parameter(typeof(TItem), "item");
            Expression property = GetPropertyExpression(parameter, filter.PropertyName);

            bool nullableProperty = IsNullableProperty(property);
            var nonNullableType = typeof(bool);

            Expression hasValue = null;

            if (nullableProperty)
            {
                hasValue = Expression.Property(property, "HasValue");
                property = Expression.Convert(property, nonNullableType);
            }

            var parsedSearchTerm = Convert.ToBoolean(filter.SearchTerm);
            var constant = Expression.Constant(parsedSearchTerm);
            Expression comparison = filter.FilterOperator switch
            {
                FilterOperator.EqualTo => Expression.Equal(property, constant),
                FilterOperator.NotEqualTo => Expression.NotEqual(property, constant),
                _ => Expression.Equal(property, constant),
            };

            Expression method = nullableProperty ?
                Expression.AndAlso(Expression.Equal(hasValue, Expression.Constant(true)), comparison) :
                comparison;

            BinaryExpression nullCkeck = Expression.Equal(parameter, Expression.Constant(null));
            Expression condition = Expression.Condition(nullCkeck, Expression.Constant(false), method);
            return Expression.Lambda<Func<TItem, bool>>(condition, parameter);
        }
        private static Expression<Func<TItem, bool>> GetExpressionDateTime<TItem>(Filter filter)
        {
            ParameterExpression parameter = Expression.Parameter(typeof(TItem), "item");
            Expression property = GetPropertyExpression(parameter, filter.PropertyName);

            bool nullableProperty = IsNullableProperty(property);
            var nonNullableType = typeof(DateTime);

            Expression hasValue = null;

            if (nullableProperty)
            {
                hasValue = Expression.Property(property, "HasValue");
                property = Expression.Convert(property, nonNullableType);
            }

            var parsedSearchTerm = Convert.ToDateTime(filter.SearchTerm, CultureInfo.InvariantCulture);
            var constant = Expression.Constant(parsedSearchTerm);

            Expression comparison = filter.FilterOperator switch
            {
                FilterOperator.EqualTo => Expression.Equal(property, constant),
                // Filter by date only without time
                //FilterOperator.EqualTo => Expression.Equal(Expression.Property(property, "Date"), Expression.Constant(parsedSearchTerm.Date)),
                FilterOperator.NotEqualTo => Expression.NotEqual(property, constant),
                FilterOperator.GreaterThan => Expression.GreaterThan(property, constant),
                FilterOperator.GreaterThanOrEqualTo => Expression.GreaterThanOrEqual(property, constant),
                FilterOperator.LessThan => Expression.LessThan(property, constant),
                FilterOperator.LessThanOrEqualTo => Expression.LessThanOrEqual(property, constant),
                _ => Expression.Equal(property, constant),
            };

            Expression method = nullableProperty ?
                Expression.AndAlso(Expression.Equal(hasValue, Expression.Constant(true)), comparison) :
                comparison;

            BinaryExpression nullCkeck = Expression.Equal(parameter, Expression.Constant(null));
            Expression condition = Expression.Condition(nullCkeck, Expression.Constant(false), method);
            return Expression.Lambda<Func<TItem, bool>>(condition, parameter);
        }
        private static Expression GetPropertyExpression(ParameterExpression parameter, string propertyName)
        {
            // Check for nested properties
            if (propertyName.IndexOf(".") > -1)
            {
                var properties = propertyName.Split('.');
                return properties.Aggregate(parameter, (Expression result, string next) => Expression.Property(result, next));
            }
            else
            {
                return Expression.Property(parameter, propertyName);
            }
        }
        private static object ParseNumeric(Type propertyType, string searchTerm, bool isNullableProperty, out Type nonNullableType)
        {
            nonNullableType = null;

            object parsedSearchTerm;

            if (isNullableProperty) // Nullable<>
            {
                if (propertyType == typeof(int?) || IsEnumOrNullableEnum(propertyType))
                {
                    nonNullableType = typeof(int);
                    parsedSearchTerm = int.Parse(searchTerm, CultureInfo.InvariantCulture);
                }
                else if (propertyType == typeof(short?))
                {
                    nonNullableType = typeof(short);
                    parsedSearchTerm = short.Parse(searchTerm, CultureInfo.InvariantCulture);
                }
                else if (propertyType == typeof(long?))
                {
                    nonNullableType = typeof(long);
                    parsedSearchTerm = long.Parse(searchTerm, CultureInfo.InvariantCulture);
                }
                else if (propertyType == typeof(decimal?))
                {
                    nonNullableType = typeof(decimal);
                    parsedSearchTerm = decimal.Parse(searchTerm, CultureInfo.InvariantCulture);
                }
                else
                {
                    nonNullableType = typeof(int);
                    parsedSearchTerm = int.Parse(searchTerm, CultureInfo.InvariantCulture);
                }
            }
            else
            {
                if (propertyType.IsEnum)
                {
                    try
                    {
                        parsedSearchTerm = Enum.ToObject(propertyType, int.Parse(searchTerm));
                    }
                    catch
                    {
                        parsedSearchTerm = Enum.ToObject(propertyType, 0);
                    }
                }
                else
                {
                    try
                    {
                        parsedSearchTerm = Convert.ChangeType(searchTerm, propertyType);
                    }
                    catch
                    {
                        parsedSearchTerm = (propertyType == typeof(long) || nonNullableType == typeof(long)) ? long.MaxValue : int.MaxValue;
                    }
                }
            }

            return parsedSearchTerm;
        }
        public static bool IsEnumOrNullableEnum(Type type) => type.IsEnum || (Nullable.GetUnderlyingType(type)?.IsEnum ?? false);
        private static bool IsNullableProperty(Expression property)
        {
            return property.Type.IsGenericType && property.Type.GetGenericTypeDefinition() == typeof(Nullable<>);
        }
    }
}
