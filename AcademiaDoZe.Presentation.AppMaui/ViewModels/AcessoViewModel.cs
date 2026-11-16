using AcademiaDoZe.Application.DTOs;
using AcademiaDoZe.Application.Enums;
using AcademiaDoZe.Application.Interfaces;
using AcademiaDoZe.Infrastructure.Exceptions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace AcademiaDoZe.Presentation.AppMaui.ViewModels;

public partial class AcessoViewModel : ObservableObject
{
    private readonly IAcessoService _acessoService;
    private string _tipoPessoaSelecionado = "Aluno";
    private string _termoBusca = string.Empty;
    private string _mensagem = "Busque uma pessoa para consultar a situação de acesso.";
    private string _mensagemHistorico = "Carregando acessos de hoje...";
    private AcessoPessoaDto? _pessoa;
    private bool _isBusy;
    private bool _mensagemErro;
    private bool _mensagemSucesso;

    public ObservableCollection<AcessoPessoaDto> PessoasEncontradas { get; } = [];
    public ObservableCollection<AcessoRegistroDto> AcessosHoje { get; } = [];

    public string TipoPessoaSelecionado
    {
        get => _tipoPessoaSelecionado;
        private set
        {
            if (SetProperty(ref _tipoPessoaSelecionado, value))
            {
                TermoBusca = string.Empty;
                SetMensagem($"Busque um(a) {value.ToLowerInvariant()} pelo nome ou CPF.", false);
            }
        }
    }

    public string TermoBusca
    {
        get => _termoBusca;
        set
        {
            if (SetProperty(ref _termoBusca, value))
            {
                Pessoa = null;
                PessoasEncontradas.Clear();
                OnPropertyChanged(nameof(TemResultados));
                SetMensagem("Busque a pessoa para consultar a situação de acesso.", false);
            }
        }
    }

    public AcessoPessoaDto? Pessoa
    {
        get => _pessoa;
        set
        {
            if (SetProperty(ref _pessoa, value))
            {
                OnPropertyChanged(nameof(TemPessoa));
                OnPropertyChanged(nameof(Situacao));
                OnPropertyChanged(nameof(UltimoAcessoTexto));
                OnPropertyChanged(nameof(Acao));
                OnPropertyChanged(nameof(Iniciais));
                OnPropertyChanged(nameof(SituacaoCor));
                OnPropertyChanged(nameof(CorBotaoAcao));
                RegistrarAcessoCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public string Mensagem
    {
        get => _mensagem;
        private set => SetProperty(ref _mensagem, value);
    }

    public string MensagemHistorico
    {
        get => _mensagemHistorico;
        private set => SetProperty(ref _mensagemHistorico, value);
    }

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

    public bool TemPessoa => Pessoa is not null;
    public bool TemResultados => PessoasEncontradas.Count > 0;
    public bool TemAcessosHoje => AcessosHoje.Count > 0;
    public bool MensagemErro => _mensagemErro;
    public string Situacao => Pessoa?.Situacao ?? string.Empty;
    public string UltimoAcessoTexto => Pessoa?.UltimoAcesso is DateTime dataHora
        ? $"Último registro: {dataHora:dd/MM/yyyy HH:mm}"
        : "Nenhum acesso registrado.";
    public string Acao => Pessoa?.ProximaAcao ?? "Registrar acesso";
    public string Iniciais => Pessoa?.Iniciais ?? string.Empty;
    public Color SituacaoCor => Pessoa?.EstaNaAcademia == true
        ? Color.FromArgb("#22C55E")
        : Color.FromArgb("#A1A1AA");
    public Brush CorBotaoAcao => new SolidColorBrush(Pessoa?.EstaNaAcademia == true
        ? Color.FromArgb("#B42318")
        : Color.FromArgb("#6D43D8"));
    public Brush MensagemFundo => new SolidColorBrush(MensagemErro
        ? Color.FromArgb("#4A1919")
        : _mensagemSucesso
            ? Color.FromArgb("#123B2B")
            : Color.FromArgb("#252533"));
    public Color MensagemTexto => MensagemErro
        ? Color.FromArgb("#FCA5A5")
        : _mensagemSucesso
            ? Color.FromArgb("#86EFAC")
            : Color.FromArgb("#E4E4E7");

    public AcessoViewModel(IAcessoService acessoService)
    {
        _acessoService = acessoService ?? throw new ArgumentNullException(nameof(acessoService));
    }

    [RelayCommand]
    private void SelecionarTipo(string tipo)
    {
        if (tipo is "Aluno" or "Colaborador")
        {
            TipoPessoaSelecionado = tipo;
        }
    }

    [RelayCommand]
    private async Task BuscarPessoaAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        Pessoa = null;
        PessoasEncontradas.Clear();
        OnPropertyChanged(nameof(TemResultados));

        try
        {
            var pessoas = await _acessoService.BuscarPessoasAsync(
                ObterTipoPessoa(),
                TermoBusca.Trim());

            foreach (var pessoa in pessoas)
            {
                PessoasEncontradas.Add(pessoa);
            }

            OnPropertyChanged(nameof(TemResultados));
            if (pessoas.Count == 1)
            {
                Pessoa = pessoas[0];
                SetMensagem($"{Pessoa.Nome} encontrada. Acesso: {Pessoa.Situacao}.", false);
            }
            else if (pessoas.Count > 1)
            {
                SetMensagem($"{pessoas.Count} pessoas encontradas. Selecione uma na lista.", false);
            }
            else
            {
                SetMensagem($"{TipoPessoaSelecionado} não encontrado(a) pelo nome ou CPF informado.", true);
            }
        }
        catch (ArgumentException ex)
        {
            SetMensagem(ex.Message, true);
        }
        catch (InfrastructureException ex)
        {
            SetMensagem($"Não foi possível consultar as pessoas: {ex.Message}", true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(PodeRegistrarAcesso))]
    private async Task RegistrarAcessoAsync()
    {
        if (Pessoa is null || IsBusy)
        {
            return;
        }

        IsBusy = true;
        try
        {
            var pessoaAtualizada = await _acessoService.RegistrarProximoAcessoAsync(Pessoa.Tipo, Pessoa.Cpf);
            int indice = PessoasEncontradas.IndexOf(Pessoa);
            if (indice >= 0)
            {
                PessoasEncontradas[indice] = pessoaAtualizada;
            }

            Pessoa = pessoaAtualizada;
            string evento = pessoaAtualizada.EstaNaAcademia ? "Entrada" : "Saída";
            SetMensagem(
                $"{evento} de {pessoaAtualizada.Nome} registrada em {pessoaAtualizada.UltimoAcesso:dd/MM/yyyy HH:mm}.",
                false,
                true);
            await AtualizarAcessosHojeAsync();
        }
        catch (ArgumentException ex)
        {
            SetMensagem(ex.Message, true);
        }
        catch (InvalidOperationException ex)
        {
            SetMensagem($"Acesso negado: {ex.Message}", true);
        }
        catch (KeyNotFoundException ex)
        {
            Pessoa = null;
            SetMensagem(ex.Message, true);
        }
        catch (InfrastructureException ex)
        {
            SetMensagem($"Não foi possível registrar o acesso: {ex.Message}", true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task CarregarAcessosHojeAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        try
        {
            await AtualizarAcessosHojeAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task AtualizarAcessosHojeAsync()
    {
        try
        {
            var registros = await _acessoService.ObterAcessosDeHojeAsync();
            AcessosHoje.Clear();
            foreach (var registro in registros)
            {
                AcessosHoje.Add(registro);
            }

            MensagemHistorico = registros.Count == 0
                ? "Nenhum acesso registrado hoje."
                : string.Empty;
            OnPropertyChanged(nameof(TemAcessosHoje));
        }
        catch (InfrastructureException ex)
        {
            MensagemHistorico = $"Não foi possível carregar os acessos: {ex.Message}";
        }
    }

    private void SetMensagem(string mensagem, bool erro, bool sucesso = false)
    {
        Mensagem = mensagem;
        if (_mensagemErro != erro || _mensagemSucesso != sucesso)
        {
            _mensagemErro = erro;
            _mensagemSucesso = sucesso;
            OnPropertyChanged(nameof(MensagemErro));
            OnPropertyChanged(nameof(MensagemFundo));
            OnPropertyChanged(nameof(MensagemTexto));
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
