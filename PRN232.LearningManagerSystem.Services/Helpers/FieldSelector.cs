namespace PRN232.LearningManagerSystem.Services.Helpers;

public static class FieldSelector
{
    public static Dictionary<string, object?> SelectFields<T>(T source, string fields)
    {
        var result = new Dictionary<string, object?>();
        var requestedFields = fields.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                    .Select(f => f.Trim())
                                    .ToList();

        var properties = typeof(T).GetProperties();

        foreach (var field in requestedFields)
        {
            var property = properties.FirstOrDefault(p =>
                string.Equals(p.Name, field, StringComparison.OrdinalIgnoreCase));

            if (property != null)
            {
                result[property.Name] = property.GetValue(source);
            }
        }

        return result;
    }
}
