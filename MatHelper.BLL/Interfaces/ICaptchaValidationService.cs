namespace MatHelper.BLL.Interfaces
{
    public interface ICaptchaValidationService
    {
        Task<bool> ValidateCaptchaAsync(string captchaToken);
    }
}
