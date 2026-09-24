using System.Windows;
using System.Windows.Controls;
using GameLauncher.ViewModels;
using UserControl = System.Windows.Controls.UserControl;

namespace GameLauncher.Views;

public partial class LoginView : UserControl
{
    public LoginView()
    {
        InitializeComponent();
    }

    private void PwdBox_OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is LoginViewModel vm && sender is PasswordBox pb)
            vm.Password = pb.Password;
    }
}
