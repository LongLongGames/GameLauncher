using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GameLauncher.Services;

namespace GameLauncher.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly IMpAuthService _auth;
    private readonly ITokenStore _tokenStore;

    [ObservableProperty]
    private string _username = "";

    [ObservableProperty]
    private string _password = "";

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _isBusy;

    public event Action? LoginSucceeded;

    public LoginViewModel(IMpAuthService auth, ITokenStore tokenStore)
    {
        _auth = auth;
        _tokenStore = tokenStore;
        // 预填上次用户名
        var last = _tokenStore.GetLastUsername();
        if (!string.IsNullOrEmpty(last))
            Username = last;
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        ErrorMessage = null;
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "请输入用户名和密码";
            return;
        }

        IsBusy = true;
        try
        {
            await _auth.LoginOfficialAsync(Username.Trim(), Password);
            LoginSucceeded?.Invoke();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }
}
