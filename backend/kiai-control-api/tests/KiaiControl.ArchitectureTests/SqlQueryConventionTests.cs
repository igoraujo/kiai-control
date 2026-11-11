using System.Reflection;
using KiaiControl.Repositories.Queries.Catalog;

namespace KiaiControl.ArchitectureTests;

public sealed class SqlQueryConventionTests
{
    [Fact]
    public void QueryClasses_ShouldExposeConstantSqlWithoutRuntimeBuilders()
    {
        var queryTypes = typeof(CatalogQueries).Assembly.GetTypes()
            .Where(type => type.Namespace?.StartsWith("KiaiControl.Repositories.Queries.", StringComparison.Ordinal) == true)
            .ToArray();

        Assert.NotEmpty(queryTypes);
        foreach (var type in queryTypes)
        {
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);
            Assert.NotEmpty(fields);
            Assert.All(fields, field => Assert.True(
                field.FieldType == typeof(string) && field.IsLiteral,
                $"{type.Name}.{field.Name} deve ser uma constante SQL."));
            Assert.DoesNotContain(
                type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly),
                method => method.ReturnType == typeof(string));
        }
    }
}
