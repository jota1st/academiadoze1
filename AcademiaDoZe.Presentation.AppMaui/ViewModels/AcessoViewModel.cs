using AcademiaDoZe.Application.DTOs;
using AcademiaDoZe.Application.Enums;
using AcademiaDoZe.Application.Interfaces;
using AcademiaDoZe.Infrastructure.Exceptions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AcademiaDoZe.Presentation.AppMaui.ViewModels;

public partial class AcessoViewModel : ObservableObject
{
    private readonly IAcessoService _acessoService;
    private string _tipoPessoaSelecionado = "Aluno";
    private string _cpf = string.Empty;
    private string _mensagem = "Informe o CPF para consultar a situação de acesso.";
    private AcessoPessoaDto? _pessoa;
    private bool _isBusy;

    public IReadOnlyList<string> TiposPessoa { get; } = ["Aluno", "Colaborador"];

    public string TipoPessoaSelecionado
    {
        get => _tipoPessoaSelecionado;
        set
        {
            if (SetProperty(ref _tipoPessoaSelecionado, value))
            {
                Pessoa = null;
                Mensagem = "Informe o CPF para consultar a situação de acesso.";
            }
        }
    }

    public string Cpf
    {
        get => _cpf;
        set
        {
            if (SetProperty(ref _cpf, value))
            {
                Pessoa = null;
            }
        }
    }

    public string Mensagem
    {
        get => _mensagem;
        private set => SetProperty(ref _mensagem, value);
    }

    public AcessoPessoaDto? Pessoa
    {
        get => _pessoa;
        private set
        {
            if (SetProperty(ref _pessoa, value))
            {
                OnPropertyChanged(nameof(TemPessoa));
                OnPropertyChanged(nameof(Situacao));
                OnPropertyChanged(nameof(UltimoAcessoTexto));
                OnPropertyChanged(nameof(Acao));
                RegistrarAcessoCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public bool TemPessoa => Pessoa is not null;
    public string Situacao => Pessoa?.Situacao ?? string.Empty;
    public string UltimoAcessoTexto => Pessoa?.UltimoAcesso is DateTime dataHora
        ? $"Último registro: {dataHora:dd/MM/yyyy HH:mm}"
        : "Nenhum acesso registrado.";
    public string Acao => Pessoa?.ProximaAcao ?? "Registrar acesso";

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                RegistrarAcessoCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public AcessoViewModel(IAcessoService acessoService)
    {
        _acessoService = acessoService ?? throw new ArgumentNullException(nameof(acessoService));
    }

    [RelayCommand]
    private async Task BuscarPessoaAsync()
    {
        IsBusy = true;
        try
        {
            var pessoa = await _acessoService.ObterPorCpfAsync(ObterTipoPessoa(), Cpf.Trim());
            Pessoa = pessoa;
            Mensagem = pessoa is null
                ? $"{TipoPessoaSelecionado} não encontrado(a) para o CPF informado."
                : $"{pessoa.Nome} — {pessoa.Situacao}.";
        }
        catch (ArgumentException ex)
        {
            Mensagem = ex.Message;
        }
        catch (InfrastructureException ex)
        {
            Mensagem = $"Não foi possível consultar o acesso: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(PodeRegistrarAcesso))]
    private async Task RegistrarAcessoAsync()
    {
        if (Pessoa is null)
        {
            return;
        }

        IsBusy = true;
        try
        {
            var pessoa = await _acessoService.RegistrarProximoAcessoAsync(ObterTipoPessoa(), Cpf.Trim());
            Pessoa = pessoa;
            string evento = pessoa.EstaNaAcademia ? "Entrada" : "Saída";
            Mensagem = $"{evento} de {pessoa.Nome} registrada em {pessoa.UltimoAcesso:dd/MM/yyyy HH:mm}.";
        }
        catch (ArgumentException ex)
        {
            Mensagem = ex.Message;
        }
        catch (InvalidOperationException ex)
        {
            Mensagem = ex.Message;
        }
        catch (KeyNotFoundException ex)
        {
            Pessoa = null;
            Mensagem = ex.Message;
        }
        catch (InfrastructureException ex)
        {
            Mensagem = $"Não foi possível registrar o acesso: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool PodeRegistrarAcesso() => Pessoa is not null && !IsBusy;

    private PessoaAcessoTipo ObterTipoPessoa() => TipoPessoaSelecionado switch
    {
        "Aluno" => PessoaAcessoTipo.Aluno,
        "Colaborador" => PessoaAcessoTipo.Colaborador,
        _ => throw new InvalidOperationException("Selecione aluno ou colaborador.")
    };
}
