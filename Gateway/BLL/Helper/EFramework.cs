
using Gateway.Data.Models;
using System.Linq.Expressions;
using System.Text;
using System.Text.Json;

namespace Gateway.BLL.Helper
{
    public class EFramework
    {
        public static IQueryable<T> ApplyDateFilters<T>(IQueryable<T> query, List<Filter> dateFilters)
        {
            foreach (var filter in dateFilters)
            {
                if (string.IsNullOrWhiteSpace(filter.Property))
                    continue;

                var propertyName = filter.Property!;
                var parameter = Expression.Parameter(typeof(T), "b");
                var property = Expression.Property(parameter, propertyName);

                // Convert Value and Value2 safely to DateTime
                DateTime startDate = GetSafeDate(filter.Value, new DateTime(1900, 1, 1));
                DateTime endDate = GetSafeDate(filter.Value2, DateTime.Now.Date.AddDays(1).AddTicks(-1));

                // Build expression: (b => b.Property >= startDate && b.Property <= endDate)
                var greaterOrEqual = Expression.GreaterThanOrEqual(property, Expression.Constant(startDate));
                var lessOrEqual = Expression.LessThanOrEqual(property, Expression.Constant(endDate));
                var between = Expression.AndAlso(greaterOrEqual, lessOrEqual);

                var lambda = Expression.Lambda<Func<T, bool>>(between, parameter);
                query = query.Where(lambda);
            }

            return query;
        }
        private static DateTime GetSafeDate(object? value, DateTime defaultValue)
        {
            if (value == null)
                return defaultValue;

            if (value is DateTime dt)
                return dt;

            if (value is JsonElement json)
            {
                if (json.ValueKind == JsonValueKind.String && DateTime.TryParse(json.GetString(), out var parsed))
                    return parsed;
                if (json.ValueKind == JsonValueKind.Number && json.TryGetInt64(out long ticks))
                    return new DateTime(ticks);
            }

            if (DateTime.TryParse(value.ToString(), out var date))
                return date;

            return defaultValue;
        }
        public static Expression<Func<T, object>> BuildPropertySelector<T>(string propertyNames)
        {
            var parameter = Expression.Parameter(typeof(T), "x");

            // Split the propertyNames string into individual properties
            var propertyNamesArray = propertyNames.Split(',');

            // Start with an empty string expression
            Expression propertyExpression = Expression.Constant(string.Empty, typeof(string));

            foreach (var propertyName in propertyNamesArray)
            {
                var trimmedPropertyName = propertyName.Trim();

                // Get the expression for the current property
                var currentPropertyExpression = GetPropertyExpression<T>(parameter, trimmedPropertyName);

                // Convert the property to its string representation
                var toStringCall = ConvertToStringExpression(currentPropertyExpression);

                // Concatenate with a space using String.Concat
                var space = Expression.Constant(" ", typeof(string));
                propertyExpression = Expression.Call(
                    typeof(string).GetMethod("Concat", [typeof(string), typeof(string)])!,
                    propertyExpression,
                    Expression.Condition(
                        Expression.Equal(toStringCall, Expression.Constant(null, typeof(string))),
                        Expression.Constant(string.Empty, typeof(string)),
                        toStringCall
                    )
                );

                // Add a space between concatenated properties (except after the last one)
                if (propertyName != propertyNamesArray.Last())
                {
                    propertyExpression = Expression.Call(
                        typeof(string).GetMethod("Concat", [typeof(string), typeof(string), typeof(string)])!,
                        propertyExpression,
                        space,
                        Expression.Constant(" ")
                    );
                }
            }

            // Convert the final expression to object
            var convert = Expression.Convert(propertyExpression, typeof(object));
            var lambda = Expression.Lambda<Func<T, object>>(convert, parameter);
            return lambda;
        }

        private static Expression ConvertToStringExpression(Expression propertyExpression)
        {
            var propertyType = propertyExpression.Type;

            // Handle nullable types
            if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                // Unwrap the nullable type
                propertyType = Nullable.GetUnderlyingType(propertyType);

                // Handle null check for nullable types
                var hasValueProperty = propertyExpression.Type.GetProperty("HasValue");
                var valueProperty = propertyExpression.Type.GetProperty("Value");
                var nullCheck = Expression.Property(propertyExpression, hasValueProperty!);
                var valueExpression = Expression.Property(propertyExpression, valueProperty!);

                // Convert the value to string if it has a value
                var toStringMethod = typeof(Convert).GetMethod("ToString", [propertyType!]);
                var toStringCall = Expression.Call(toStringMethod!, valueExpression);

                return Expression.Condition(
                    nullCheck,
                    toStringCall,
                    Expression.Constant(string.Empty, typeof(string))
                );
            }

            // Convert non-nullable property to its string representation
            var nonNullableToStringMethod = typeof(Convert).GetMethod("ToString", [propertyType]) ?? throw new InvalidOperationException($"Cannot convert property of type '{propertyType.Name}' to string.");

            return Expression.Call(nonNullableToStringMethod, propertyExpression);
        }

        private static Expression GetPropertyExpression<T>(Expression parameter, string propertyName)
        {
            var propertyType = typeof(T);
            var propertyExpression = parameter;

            foreach (var prop in propertyName.Split('.'))
            {
                var propertyInfo = propertyType.GetProperty(prop) ?? throw new ArgumentException($"Property '{prop}' not found on type '{propertyType.Name}'.");
                propertyExpression = Expression.Property(propertyExpression, propertyInfo);
                propertyType = propertyInfo.PropertyType;
            }

            return propertyExpression;
        }

        public static string GetEntityProperties<T>(T entity) where T : class
        {
            var entityType = entity.GetType();

            var stringBuilder = new StringBuilder();

            foreach (var property in entityType.GetProperties())
            {
                var propertyName = property.Name;

                if (!property.GetCustomAttributes(true).Any(a => a.GetType().Name == "NotMappedAttribute") &&
                    property.GetCustomAttributes(true).All(a => a.GetType().Name != "JsonIgnoreAttribute"))
                {
                    var propertyValue = property.GetValue(entity);

                    stringBuilder.Append($"[{propertyName}: {propertyValue}]");
                }
            }

            return stringBuilder.ToString();
        }
    }
}
