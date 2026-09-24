using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GameLauncher.Models;
using GameLauncher.Services;

namespace GameLauncher.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly ICatalogService _catalog;
    private readonly SessionState _session;
    private readonly GameDetailViewModel _detail;

    [ObservableProperty]
    private ObservableCollection<GameCatalogItem> _games = new();

    [ObservableProperty]
    private GameCatalogItem? _selectedGame;

    [ObservableProperty]
    private string _userDisplay = "";

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _isLoading;

    public GameDetailViewModel Detail => _detail;

    public MainViewModel(ICatalogService catalog, SessionState session, GameDetailViewModel detail)
    {
        _catalog = catalog;
        _session = session;
        _detail = detail;
        UserDisplay = session.User?.DisplayName
                      ?? session.User?.Username
                      ?? "玩家";
    }

    partial void OnSelectedGameChanged(GameCatalogItem? value)
    {
        _detail.Bind(value);
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsLoading = true;
        ErrorMessage = null;
        try
        {
            var list = await _catalog.GetActiveGamesAsync();
            Games = new ObservableCollection<GameCatalogItem>(list);
            SelectedGame = Games.FirstOrDefault();
        }
        catch (Exception ex)
        {
            ErrorMessage = "拉取游戏列表失败: " + ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void Logout()
    {
        _session.Clear();
        LogoutRequested?.Invoke();
    }

    public event Action? LogoutRequested;
}
