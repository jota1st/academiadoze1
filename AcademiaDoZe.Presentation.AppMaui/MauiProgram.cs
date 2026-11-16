#if MAUI_DEVFLOW
using Microsoft.Maui.DevFlow.Agent;
#endif
using AcademiaDoZe.Presentation.AppMaui.ViewModels;
using AcademiaDoZe.Presentation.AppMaui.Views;
using Microsoft.Extensions.Logging;
using AcademiaDoZe.Presentation.AppMaui.Configuration;
namespace AcademiaDoZe.Presentation.AppMaui;
public static class MauiProgram
{
public static MauiApp CreateMauiApp()
{
var builder = MauiApp.CreateBuilder();
#if MAUI_DEVFLOW
builder.AddMauiDevFlowAgent();
#endif
builder
.UseMauiApp<App>()
.ConfigureFonts(fonts =>
{
fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
// Adicione esta linha para registrar a fonte
fonts.AddFont("MaterialIcons-Regular.ttf", "MaterialIcons");
});
// Configurar serviços da aplicação e repositórios
ConfigurationHelper.ConfigureServices(builder.Services);
// Registrar ViewModels
builder.Services.AddTransient<ColaboradorListViewModel>();
builder.Services.AddTransient<ColaboradorViewModel>();
builder.Services.AddTransient<MatriculaListViewModel>();
builder.Services.AddTransient<MatriculaViewModel>();
builder.Services.AddTransient<AcessoViewModel>();

// Registrar Views
builder.Services.AddTransient<DashboardListPage>();
builder.Services.AddTransient<ConfigPage>();
builder.Services.AddTransient<ColaboradorListPage>();
builder.Services.AddTransient<ColaboradorPage>();
builder.Services.AddTransient<MatriculaListPage>();
builder.Services.AddTransient<MatriculaPage>();
builder.Services.AddTransient<AcessoPage>();
#if DEBUG
builder.Logging.AddDebug();
#endif
return builder.Build();
}
}