public static class RouteTemplateHelper
{
    public static string StripWildcards(string pathTemplate)
    {
        if (string.IsNullOrWhiteSpace(pathTemplate))
            return string.Empty;

        return pathTemplate
            .Replace("{**catch-all}", "", StringComparison.OrdinalIgnoreCase)
            .Replace("{*catch-all}", "", StringComparison.OrdinalIgnoreCase)
            .Replace("{**remainder}", "", StringComparison.OrdinalIgnoreCase)
            .Replace("{*remainder}", "", StringComparison.OrdinalIgnoreCase)
            .TrimEnd('/');
    }
}