namespace MatHelper.CORE.Models
{
    public class RegistrationsDto
    {
        public required DateOnly Date { get; set; }
        public required ushort Registrations { get; set; }
    }
}