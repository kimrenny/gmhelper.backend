namespace MatHelper.CORE.Models
{
    public class SwitchUpdateRequest
    {
        public required string SectionTitle { get; set; }
        public required string SwitchLabel { get; set; }
        public required bool NewValue { get; set; }
    }
}