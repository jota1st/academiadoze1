using Microsoft.Maui;
using Microsoft.Maui.Controls;

namespace AcademiaDoZe.Presentation.AppMaui
{
    // Application conflita com o nome da nossa camada de aplicação.
    // O alias garante que a classe base seja a Application do .NET MAUI.
    using MauiApplication = Microsoft.Maui.Controls.Application;

    public partial class App : MauiApplication
    {
        public App()
        {
            InitializeComponent();
        }
        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }
    }
}