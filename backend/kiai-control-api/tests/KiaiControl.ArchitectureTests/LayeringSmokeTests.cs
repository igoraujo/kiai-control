using System.Xml.Linq;

namespace KiaiControl.ArchitectureTests;

public sealed class LayeringTests
{
    [Fact]
    public void Project_references_ShouldFollowTheAllowedLayerGraph()
    {
        var allowedProjectReferences = new Dictionary<string, string[]>
        {
            ["KiaiControl.Api"] = ["KiaiControl.Contracts", "KiaiControl.Core", "KiaiControl.Repositories", "KiaiControl.Services", "KiaiControl.UseCases"],
            ["KiaiControl.Contracts"] = [],
            ["KiaiControl.Core"] = [],
            ["KiaiControl.Repositories"] = ["KiaiControl.Core"],
            ["KiaiControl.Services"] = ["KiaiControl.Core"],
            ["KiaiControl.UseCases"] = ["KiaiControl.Contracts", "KiaiControl.Core", "KiaiControl.Repositories", "KiaiControl.Services"]
        };
        var apiRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

        foreach (var (projectName, expectedReferences) in allowedProjectReferences)
        {
            var projectFile = Path.Combine(apiRoot, "src", projectName, $"{projectName}.csproj");
            var actualReferences = XDocument.Load(projectFile)
                .Descendants("ProjectReference")
                .Select(reference => Path.GetFileNameWithoutExtension(
                    ((string?)reference.Attribute("Include") ?? string.Empty).Replace('\\', '/')))
                .OrderBy(reference => reference, StringComparer.Ordinal);
            var orderedExpectedReferences = expectedReferences.OrderBy(reference => reference, StringComparer.Ordinal);

            Assert.Equal(orderedExpectedReferences, actualReferences);
        }
    }
}
