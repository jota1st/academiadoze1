using AcademiaDoZe.Presentation.AppMaui.ViewModels;

namespace AcademiaDoZe.Presentation.AppMaui.Views;

public partial class AcessoPage : ContentPage
{
    public AcessoPage(AcessoViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is not AcessoViewModel viewModel)
            return;

        var command = viewModel.GetType().GetProperty("CarregarAcessosHojeCommand")?.GetValue(viewModel);
        if (command is null)
            return;

        var executeAsync = command.GetType().GetMethod("ExecuteAsync", new[] { typeof(object) });
        if (executeAsync is not null)
        {
            var result = executeAsync.Invoke(command, [null]);
            if (result is Task task)
            {
                await task;
                return;
            }

            return;
        }

        var execute = command.GetType().GetMethod("Execute");
        if (execute is not null)
        {
            execute.Invoke(command, [null]);
        }
    }
}
