namespace MatHelper.CORE.Models
{
    public class SwitchUpdateRequest
    {
        public required int SectionId { get; set; }
        public required string SwitchLabel { get; set; }
        public required bool NewValue { get; set; }
    }
}