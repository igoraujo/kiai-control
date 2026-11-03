using KiaiControl.Api.Controllers;
using KiaiControl.Contracts.Modalities;
using KiaiControl.Core.Common;
using KiaiControl.Repositories.Implementations;
using KiaiControl.UseCases.Modalities;
using Microsoft.AspNetCore.Mvc;

namespace KiaiControl.UnitTests;

public sealed class WorkspaceControllerTests
{
    [Theory]
    [InlineData("martial_art", true, false, true)]
    [InlineData("fitness", false, true, false)]
    [InlineData("hybrid", true, true, false)]
    public async Task GetCapabilitiesAsync_ShouldReflectActiveModalityTypes(
        string modalityType,
        bool hasMartialArts,
        bool hasFitnessActivities,
        bool hasGraduation)
    {
        var organizationId = Guid.NewGuid();
        var modalityUseCase = new ModalityUseCase(new InMemoryModalityRepository());
        await modalityUseCase.CreateAsync(organizationId, new CreateModalityRequest
        {
            Name = "Modalidade de teste",
            Type = modalityType
        }, CancellationToken.None);
        var controller = new WorkspaceController(
            new OrganizationContext { OrganizationId = organizationId },
            modalityUseCase);

        var action = await controller.GetCapabilitiesAsync(CancellationToken.None);

        var response = Assert.IsType<WorkspaceCapabilitiesResponse>(Assert.IsType<OkObjectResult>(action.Result).Value);
        Assert.Equal(hasMartialArts, response.HasMartialArts);
        Assert.Equal(hasFitnessActivities, response.HasFitnessActivities);
        Assert.Equal(hasGraduation, response.HasGraduation);
    }

    [Fact]
    public async Task GetCapabilitiesAsync_ShouldRequireOrganizationContext()
    {
        var controller = new WorkspaceController(
            new OrganizationContext(),
            new ModalityUseCase(new InMemoryModalityRepository()));

        var action = await controller.GetCapabilitiesAsync(CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(action.Result);
    }
}
