using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;

namespace Outsourced.DataCube;

internal static class InternalExtensions
{
  public static string GetPropertyName<T, TProperty>(this Expression<Func<T, TProperty>> expression)
  {
    return GetMemberName(expression.Body);
  }

  public static string GetPropertyName<T>(this Expression<Func<T, object>> expression)
  {
    return GetMemberName(expression.Body);
  }

  private static string GetMemberName(Expression expression)
  {
    if (expression is MemberExpression memberExpression)
    {
      return memberExpression.Member.Name;
    }

    if (expression is UnaryExpression unaryExpression && unaryExpression.Operand is MemberExpression innerMemberExpression)
    {
      return innerMemberExpression.Member.Name;
    }

    throw new ArgumentException("Expression is not a member access", nameof(expression));
  }

  public static string GetPropertyValueAsString<TEntity, TProperty>(this TEntity entity, Expression<Func<TEntity, TProperty>> propertySelector)
  {
    if (entity == null) return Constants.NULL_LABEL;

    var func = propertySelector.Compile();
    var value = func(entity);

    return value?.ToString() ?? Constants.NULL_LABEL;
  }

  public static T ToEntity<T>(this IDictionary<string, object> values) where T : new()
  {
    var type = typeof(T);
    object entity = new T();

    foreach (var kvp in values)
    {
      var property = type.GetProperty(kvp.Key, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
      if (property != null && property.CanWrite)
      {
        property.SetValue(entity, ConvertPropertyValue(kvp.Value, property.PropertyType));
      }
    }

    return (T)entity;
  }

  private static object ConvertPropertyValue(object value, Type propertyType)
  {
    if (value == null)
      return GetDefaultValue(propertyType);

    var targetType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;
    if (targetType.IsInstanceOfType(value))
      return value;

    if (targetType.IsEnum)
    {
      if (value is string enumText)
        return Enum.Parse(targetType, enumText, ignoreCase: true);

      var enumValue = Convert.ChangeType(value, Enum.GetUnderlyingType(targetType), CultureInfo.InvariantCulture);
      return Enum.ToObject(targetType, enumValue);
    }

    if (value is IConvertible && typeof(IConvertible).IsAssignableFrom(targetType))
      return Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);

    return value;
  }

  private static object GetDefaultValue(Type propertyType)
  {
    return Nullable.GetUnderlyingType(propertyType) == null && propertyType.IsValueType
      ? Activator.CreateInstance(propertyType)
      : null;
  }
}
