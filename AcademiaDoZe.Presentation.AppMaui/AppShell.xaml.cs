using AcademiaDoZe.Presentation.AppMaui.Views;

namespace AcademiaDoZe.Presentation.AppMaui;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        RegisterRoutes();
    }

    // O Routing.RegisterRoute é necessário para que o Shell do MAUI reconheça a navegação
    // para páginas não diretamente expostas no Flyout/TabBar, como cadastro e edição.
    private static void RegisterRoutes()
    {
        Routing.RegisterRoute("colaborador", typeof(ColaboradorPage));
        Routing.RegisterRoute("matricula", typeof(MatriculaPage));
        Routing.RegisterRoute("matriculas", typeof(MatriculaListPage));
    }
}