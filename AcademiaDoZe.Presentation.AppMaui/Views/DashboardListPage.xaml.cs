namespace AcademiaDoZe.Presentation.AppMaui.Views;

public partial class DashboardListPage : ContentPage
{
    public DashboardListPage()
    {
        InitializeComponent();
    }

    private async void OnOpenColaboradoresClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//colaboradores");
    }

    private async void OnOpenConfiguracoesClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//configuracoes");
    }

    private async void OnOpenAcessosClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//acessos");
    }
}
