namespace LaptopStore.Models.ViewModels
{
    public class AuthViewModel
    {
        public LoginViewModel Login { get; set; } = new LoginViewModel();
        public RegisterViewModel Register { get; set; } = new RegisterViewModel();
        public string ActiveTab { get; set; } = "login";
    }
}
