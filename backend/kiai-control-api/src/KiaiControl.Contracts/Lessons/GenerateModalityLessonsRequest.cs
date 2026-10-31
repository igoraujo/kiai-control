namespace KiaiControl.Contracts.Lessons;

public sealed class GenerateModalityLessonsRequest
{
    public Guid ModalityId { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }
}
