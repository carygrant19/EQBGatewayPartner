using Gateway.Data.Models;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;

public class ExpressionBuilder
{
    private static readonly MethodInfo containsMethod = typeof(string).GetMethod("Contains", [typeof(string)])!;
    private static readonly MethodInfo startsWithMethod = typeof(string).GetMethod("StartsWith", [typeof(string)])!;
    private static readonly MethodInfo endsWithMethod = typeof(string).GetMethod("EndsWith", [typeof(string)])!;

    public static Expression<Func<T, bool>>? GetExpression<T>(IList<Filter> filters)
    {
        if (filters == null || filters.Count == 0)
            return null;

        ParameterExpression param = Expression.Parameter(typeof(T), "t");
        Expression? exp = null;

        foreach (var filter in filters)
        {
            // Support for Multiple Columns via OR
            var currentExpression = GetFilterExpression(param, filter);

            if (currentExpression != null)
                exp = exp == null ? currentExpression : Expression.AndAlso(exp, currentExpression);
        }

        return exp == null ? null : Expression.Lambda<Func<T, bool>>(exp, param);
    }

    private static Expression? GetFilterExpression(ParameterExpression param, Filter filter)
    {
        if (string.IsNullOrWhiteSpace(filter.Property)) return null;

        // If Property contains commas, split and join with OR (OrElse)
        if (filter.Property.Contains(','))
        {
            var properties = filter.Property.Split(',', StringSplitOptions.RemoveEmptyEntries);
            Expression? orExp = null;

            foreach (var prop in properties)
            {
                var subFilter = new Filter
                {
                    Property = prop.Trim(),
                    Operator = filter.Operator,
                    Value = filter.Value,
                    Value2 = filter.Value2,
                    Values = filter.Values,
                    IsDate = filter.IsDate
                };

                var singleExp = BuildComparison(param, subFilter);
                orExp = orExp == null ? singleExp : Expression.OrElse(orExp, singleExp);
            }
            return orExp;
        }

        return BuildComparison(param, filter);
    }

    private static Expression BuildComparison(ParameterExpression param, Filter filter)
    {
        Expression member = GetNestedPropertyExpression(param, filter.Property!);

        switch (filter.Operator!.ToUpper())
        {
            case Operators.Equals:
                return Expression.Equal(member, ConvertValueType(member, filter.Value!));

            case Operators.NotEquals:
                return Expression.NotEqual(member, ConvertValueType(member, filter.Value!));

            case Operators.GreaterThan:
                return Expression.GreaterThan(member, ConvertValueType(member, filter.Value!));

            case Operators.GreaterThanOrEqual:
                return Expression.GreaterThanOrEqual(member, ConvertValueType(member, filter.Value!));

            case Operators.LessThan:
                return Expression.LessThan(member, ConvertValueType(member, filter.Value!));

            case Operators.LessThanOrEqual:
                return Expression.LessThanOrEqual(member, ConvertValueType(member, filter.Value!));

            case Operators.Contains:
                if (member.Type != typeof(string))
                    throw new NotSupportedException("Contains is only supported for string fields");

                if (filter.Values != null && filter.Values.Any())
                {
                    var containsExpressions = filter.Values.Select(v =>
                        (Expression)Expression.Call(member, containsMethod, ConvertValueType(member, v)));
                    return containsExpressions.Aggregate(Expression.OrElse);
                }
                return Expression.Call(member, containsMethod, ConvertValueType(member, filter.Value!));

            case Operators.StartsWith:
                return Expression.Call(member, startsWithMethod, ConvertValueType(member, filter.Value!));

            case Operators.EndsWith:
                return Expression.Call(member, endsWithMethod, ConvertValueType(member, filter.Value!));

            case Operators.Between:
                return BuildBetweenExpression(member, filter);

            case Operators.In:
                var inValues = filter.Values!.Select(v => Expression.Equal(member, ConvertValueType(member, v)));
                return inValues.Aggregate(Expression.OrElse);

            case Operators.IsNull:
                return Expression.Equal(member, Expression.Constant(null, member.Type));

            case Operators.IsNotNull:
                return Expression.NotEqual(member, Expression.Constant(null, member.Type));

            default:
                throw new NotSupportedException($"The operator '{filter.Operator}' is not supported");
        }
    }

    private static Expression BuildBetweenExpression(Expression member, Filter filter)
    {
        if (filter.IsDate)
        {
            DateTime startDate = string.IsNullOrEmpty(Convert.ToString(filter.Value))
                ? new DateTime(1900, 1, 1)
                : DateTime.ParseExact(Convert.ToString(filter.Value)!, "yyyy-MM-dd", CultureInfo.InvariantCulture);

            DateTime endDate = string.IsNullOrEmpty(Convert.ToString(filter.Value2))
                ? DateTime.Today.AddDays(1).AddTicks(-1)
                : DateTime.ParseExact(Convert.ToString(filter.Value2)!, "yyyy-MM-dd", CultureInfo.InvariantCulture).Date.AddDays(1).AddTicks(-1);

            var targetType = Nullable.GetUnderlyingType(member.Type) ?? member.Type;
            var lower = Expression.Constant(startDate, targetType);
            var upper = Expression.Constant(endDate, targetType);

            Expression memberExpr = member.Type != targetType ? Expression.Convert(member, targetType) : member;

            return Expression.AndAlso(
                Expression.GreaterThanOrEqual(memberExpr, lower),
                Expression.LessThanOrEqual(memberExpr, upper)
            );
        }

        return Expression.AndAlso(
            Expression.GreaterThanOrEqual(member, ConvertValueType(member, filter.Value!)),
            Expression.LessThanOrEqual(member, ConvertValueType(member, filter.Value2!))
        );
    }

    private static Expression GetNestedPropertyExpression(Expression param, string propertyPath)
    {
        Expression member = param;
        foreach (var property in propertyPath.Split('.'))
        {
            member = Expression.Property(member, property);
        }
        return member;
    }

    private static Expression ConvertValueType(Expression member, object? value)
    {
        if (value is JsonElement jsonElement)
        {
            switch (jsonElement.ValueKind)
            {
                case JsonValueKind.Number:
                    if (member.Type == typeof(int) || member.Type == typeof(int?)) value = jsonElement.GetInt32();
                    else if (member.Type == typeof(double) || member.Type == typeof(double?)) value = jsonElement.GetDouble();
                    else if (member.Type == typeof(decimal) || member.Type == typeof(decimal?)) value = (decimal)jsonElement.GetDouble();
                    break;
                case JsonValueKind.String: value = jsonElement.GetString(); break;
                case JsonValueKind.True: value = true; break;
                case JsonValueKind.False: value = false; break;
                case JsonValueKind.Null: value = null; break;
            }
        }

        var underlyingType = Nullable.GetUnderlyingType(member.Type) ?? member.Type;
        return ConvertToExpression(underlyingType, value!, member.Type);
    }

    private static Expression ConvertToExpression(Type targetType, object value, Type memberType)
    {
        if (value == null) return Expression.Constant(null, memberType);

        object convertedValue;
        if (targetType == typeof(DateTime))
            convertedValue = DateTime.ParseExact(Convert.ToString(value)!, "yyyy-MM-dd", CultureInfo.InvariantCulture).Date;
        else
            convertedValue = Convert.ChangeType(value, targetType);

        return Expression.Constant(convertedValue, memberType);
    }
}