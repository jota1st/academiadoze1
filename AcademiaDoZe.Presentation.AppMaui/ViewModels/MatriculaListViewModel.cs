using AcademiaDoZe.Application.DTOs;
using AcademiaDoZe.Application.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace AcademiaDoZe.Presentation.AppMaui.ViewModels;

public partial class MatriculaListViewModel : ObservableObject
{
    public ObservableCollection<string> FilterTypes { get; } = ["Id", "Id Aluno", "Ativas", "Vencendo em dias"];

    private readonly IMatriculaService _matriculaService;
    private string _searchText = string.Empty;
    private string _selectedFilterType = "Ativas";
    private ObservableCollection<MatriculaDto> _matriculas = [];
    private MatriculaDto? _selectedMatricula;
    private bool _isBusy;
    private bool _isRefreshing;
    private string _title = string.Empty;

    public string SearchText
    {
        get => _searchText;
        set => SetProperty(ref _searchText, value);
    }

    public string SelectedFilterType
    {
        get => _selectedFilterType;
        set => SetProperty(ref _selectedFilterType, value);
    }

    public ObservableCollection<MatriculaDto> Matriculas
    {
        get => _matriculas;
        set => SetProperty(ref _matriculas, value);
    }

    public MatriculaDto? SelectedMatricula
    {
        get => _selectedMatricula;
        set => SetProperty(ref _selectedMatricula, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }

    public bool IsRefreshing
    {
        get => _isRefreshing;
        set => SetProperty(ref _isRefreshing, value);
    }

    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    public MatriculaListViewModel(IMatriculaService matriculaService)
    {
        _matriculaService = matriculaService;
        Title = "Matriculas";
    }

    [RelayCommand]
    private async Task AddMatriculaAsync()
    {
        try
        {
            await Shell.Current.GoToAsync("matricula");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Erro", $"Erro ao navegar para tela de cadastro: {ex.Message}", "OK");
        }
    }

    [RelayCommand]
    private async Task EditMatriculaAsync(MatriculaDto? matricula)
    {
        try
        {
            if (matricula is null)
                return;

            await Shell.Current.GoToAsync($"matricula?Id={matricula.Id}");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Erro", $"Erro ao navegar para tela de edição: {ex.Message}", "OK");
        }
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        IsRefreshing = true;
        await LoadMatriculasAsync();
    }

    [RelayCommand]
    private async Task SearchMatriculasAsync()
    {
        if (IsBusy)
            return;

        try
        {
            IsBusy = true;
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                Matriculas.Clear();
                OnPropertyChanged(nameof(Matriculas));
            });

            IEnumerable<MatriculaDto> resultados = [];

            if (string.IsNullOrWhiteSpace(SearchText))
            {
                if (SelectedFilterType == "Ativas")
                    resultados = await _matriculaService.ObterAtivasAsync(0, cts.Token) ?? [];
                else
                    resultados = await _matriculaService.ObterTodasAsync(cts.Token) ?? [];
            }
            else if (SelectedFilterType == "Id")
            {
                if (int.TryParse(SearchText.Trim(), out var id))
                {
                    var matricula = await _matriculaService.ObterPorIdAsync(id, cts.Token);
                    if (matricula is not null)
                        resultados = [matricula];
                }
                else
                {
                    await Shell.Current.DisplayAlertAsync("Aviso", "Para busca por ID, informe um número válido.", "OK");
                    return;
                }
            }
            else if (SelectedFilterType == "Id Aluno")
            {
                if (int.TryParse(SearchText.Trim(), out var alunoId))
                {
                    resultados = await _matriculaService.ObterPorAlunoIdAsync(alunoId, cts.Token) ?? [];
                }
                else
                {
                    await Shell.Current.DisplayAlertAsync("Aviso", "Para busca por ID do Aluno, informe um número válido.", "OK");
                    return;
                }
            }
            else if (SelectedFilterType == "Ativas")
            {
                int.TryParse(SearchText.Trim(), out var alunoId);
                resultados = await _matriculaService.ObterAtivasAsync(alunoId, cts.Token) ?? [];
            }
            else if (SelectedFilterType == "Vencendo em dias")
            {
                if (int.TryParse(SearchText.Trim(), out var dias))
                {
                    resultados = await _matriculaService.ObterVencendoEmDiasAsync(dias, cts.Token) ?? [];
                }
                else
                {
                    await Shell.Current.DisplayAlertAsync("Aviso", "Para busca por vencimento, informe a quantidade de dias (número inteiro).", "OK");
                    return;
                }
            }

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                foreach (var item in resultados)
                    Matriculas.Add(item);

                OnPropertyChanged(nameof(Matriculas));
            });
        }
        catch (OperationCanceledException)
        {
            await Shell.Current.DisplayAlertAsync("Tempo Esgotado", "A busca de matrículas expirou. Verifique a conexão com o banco.", "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Erro", $"Erro ao buscar matrículas: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task LoadMatriculasAsync()
    {
        if (IsBusy)
            return;

        try
        {
            IsBusy = true;
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                Matriculas.Clear();
                OnPropertyChanged(nameof(Matriculas));
            });

            var matriculasList = await _matriculaService.ObterTodasAsync(cts.Token);
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                foreach (var matricula in matriculasList)
                    Matriculas.Add(matricula);

                OnPropertyChanged(nameof(Matriculas));
            });
        }
        catch (OperationCanceledException)
        {
            await Shell.Current.DisplayAlertAsync("Tempo Esgotado", "O carregamento de matrículas expirou. Verifique a conexão com o banco.", "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Erro", $"Erro ao carregar matrículas: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    private async Task DeleteMatriculaAsync(MatriculaDto? matricula)
    {
        if (matricula is null)
            return;

        var confirm = await Shell.Current.DisplayAlertAsync(
            "Confirmar Exclusão",
            $"Deseja realmente excluir a matrícula {matricula.Id}?",
            "Sim",
            "Não");

        if (!confirm)
            return;

        try
        {
            IsBusy = true;
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            var success = await _matriculaService.RemoverAsync(matricula.Id, cts.Token);
            if (success)
            {
                Matriculas.Remove(matricula);
                await Shell.Current.DisplayAlertAsync("Sucesso", "Matrícula excluída com sucesso!", "OK");
            }
            else
            {
                await Shell.Current.DisplayAlertAsync("Erro", "Não foi possível excluir a matrícula.", "OK");
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Erro", $"Erro ao excluir matrícula: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }
}