using AcademiaDoZe.Application.DTOs;
using AcademiaDoZe.Application.Enums;
using AcademiaDoZe.Application.Interfaces;
using AcademiaDoZe.Domain.Entities;
using AcademiaDoZe.Domain.Repositories;
using AcademiaDoZe.Domain.ValueObjects;

namespace AcademiaDoZe.Application.Services;

public sealed class AcessoService : IAcessoService
{
    private readonly Func<IAlunoRepository> _alunoRepositoryFactory;
    private readonly Func<IColaboradorRepository> _colaboradorRepositoryFactory;
    private readonly Func<IAcessoAlunoRepository> _acessoAlunoRepositoryFactory;
    private readonly Func<IAcessoColaboradorRepository> _acessoColaboradorRepositoryFactory;
    private readonly Func<IMatriculaRepository> _matriculaRepositoryFactory;
    private readonly TimeProvider _timeProvider;

    public async Task<IReadOnlyList<AcessoPessoaDto>> BuscarPessoasAsync(
        PessoaAcessoTipo tipo,
        string termo,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(termo))
        {
            throw new ArgumentException("Informe um nome ou CPF para buscar.", nameof(termo));
        }

        string busca = termo.Trim();
        string buscaNumerica = ObterDigitos(busca);
        var pessoas = new List<AcessoPessoaDto>();

        switch (tipo)
        {
            case PessoaAcessoTipo.Aluno:
            {
                var alunos = await _alunoRepositoryFactory().ObterTodos(cancellationToken);
                var acessoRepository = _acessoAlunoRepositoryFactory();
                foreach (var aluno in alunos.Where(aluno => Corresponde(aluno.Nome, aluno.Cpf.Valor, busca, buscaNumerica))
                             .OrderBy(aluno => aluno.Nome, StringComparer.CurrentCultureIgnoreCase))
                {
                    bool estaNaAcademia = await acessoRepository.EstaNaAcademia(aluno.Id, cancellationToken);
                    var ultimoAcesso = await acessoRepository.ObterUltimoAcesso(aluno.Id, cancellationToken);
                    pessoas.Add(new AcessoPessoaDto
                    {
                        Id = aluno.Id,
                        Nome = aluno.Nome,
                        Cpf = aluno.Cpf.Valor,
                        Tipo = PessoaAcessoTipo.Aluno,
                        EstaNaAcademia = estaNaAcademia,
                        UltimoAcesso = ultimoAcesso?.DataHora
                    });
                }

                break;
            }
            case PessoaAcessoTipo.Colaborador:
            {
                var colaboradores = await _colaboradorRepositoryFactory().ObterTodos(cancellationToken);
                var acessoRepository = _acessoColaboradorRepositoryFactory();
                foreach (var colaborador in colaboradores.Where(colaborador =>
                                 Corresponde(colaborador.Nome, colaborador.Cpf.Valor, busca, buscaNumerica))
                             .OrderBy(colaborador => colaborador.Nome, StringComparer.CurrentCultureIgnoreCase))
                {
                    bool estaNaAcademia = await acessoRepository.EstaNaAcademia(colaborador.Id, cancellationToken);
                    var ultimoAcesso = await acessoRepository.ObterUltimoAcesso(colaborador.Id, cancellationToken);
                    pessoas.Add(new AcessoPessoaDto
                    {
                        Id = colaborador.Id,
                        Nome = colaborador.Nome,
                        Cpf = colaborador.Cpf.Valor,
                        Tipo = PessoaAcessoTipo.Colaborador,
                        EstaNaAcademia = estaNaAcademia,
                        UltimoAcesso = ultimoAcesso?.DataHora
                    });
                }

                break;
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(tipo), tipo, "Tipo de pessoa inválido.");
        }

        return pessoas;
    }

    public AcessoService(
        Func<IAlunoRepository> alunoRepositoryFactory,
        Func<IColaboradorRepository> colaboradorRepositoryFactory,
        Func<IAcessoAlunoRepository> acessoAlunoRepositoryFactory,
        Func<IAcessoColaboradorRepository> acessoColaboradorRepositoryFactory,
        Func<IMatriculaRepository> matriculaRepositoryFactory,
        TimeProvider? timeProvider = null)
    {
        _alunoRepositoryFactory = alunoRepositoryFactory ?? throw new ArgumentNullException(nameof(alunoRepositoryFactory));
        _colaboradorRepositoryFactory = colaboradorRepositoryFactory ?? throw new ArgumentNullException(nameof(colaboradorRepositoryFactory));
        _acessoAlunoRepositoryFactory = acessoAlunoRepositoryFactory ?? throw new ArgumentNullException(nameof(acessoAlunoRepositoryFactory));
        _acessoColaboradorRepositoryFactory = acessoColaboradorRepositoryFactory ?? throw new ArgumentNullException(nameof(acessoColaboradorRepositoryFactory));
        _matriculaRepositoryFactory = matriculaRepositoryFactory ?? throw new ArgumentNullException(nameof(matriculaRepositoryFactory));
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<AcessoPessoaDto?> ObterPorCpfAsync(
        PessoaAcessoTipo tipo,
        string cpf,
        CancellationToken cancellationToken = default)
    {
        var cpfValue = CriarCpf(cpf);
        return tipo switch
        {
            PessoaAcessoTipo.Aluno => await ObterAlunoAsync(cpfValue, cancellationToken),
            PessoaAcessoTipo.Colaborador => await ObterColaboradorAsync(cpfValue, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(tipo), tipo, "Tipo de pessoa inválido.")
        };
    }

    public async Task<IReadOnlyList<AcessoRegistroDto>> ObterAcessosDeHojeAsync(
        CancellationToken cancellationToken = default)
    {
        DateOnly hoje = DateOnly.FromDateTime(_timeProvider.GetLocalNow().DateTime);
        var registros = new List<AcessoRegistroDto>();

        var repositorioAlunos = _acessoAlunoRepositoryFactory();
        var acessosAlunos = await repositorioAlunos
            .ObterAcessosPorAlunoPeriodo(inicio: hoje, fim: hoje, cancellationToken: cancellationToken);
        foreach (var grupo in acessosAlunos.GroupBy(acesso => acesso.Aluno.Id))
        {
            bool estadoAtual = await repositorioAlunos.EstaNaAcademia(grupo.Key, cancellationToken);
            foreach (var acesso in grupo.OrderByDescending(acesso => acesso.DataHora).ThenByDescending(acesso => acesso.Id))
            {
                registros.Add(new AcessoRegistroDto
                {
                    Nome = acesso.Aluno.Nome,
                    TipoPessoa = PessoaAcessoTipo.Aluno,
                    DataHora = acesso.DataHora,
                    Entrada = estadoAtual
                });
                estadoAtual = !estadoAtual;
            }
        }

        var repositorioColaboradores = _acessoColaboradorRepositoryFactory();
        var acessosColaboradores = await repositorioColaboradores
            .ObterAcessosPorColaboradorPeriodo(inicio: hoje, fim: hoje, cancellationToken: cancellationToken);
        foreach (var grupo in acessosColaboradores.GroupBy(acesso => acesso.Colaborador.Id))
        {
            bool estadoAtual = await repositorioColaboradores.EstaNaAcademia(grupo.Key, cancellationToken);
            foreach (var acesso in grupo.OrderByDescending(acesso => acesso.DataHora).ThenByDescending(acesso => acesso.Id))
            {
                registros.Add(new AcessoRegistroDto
                {
                    Nome = acesso.Colaborador.Nome,
                    TipoPessoa = PessoaAcessoTipo.Colaborador,
                    DataHora = acesso.DataHora,
                    Entrada = estadoAtual
                });
                estadoAtual = !estadoAtual;
            }
        }

        return registros
            .OrderByDescending(registro => registro.DataHora)
            .ToArray();
    }

    public async Task<AcessoPessoaDto> RegistrarProximoAcessoAsync(
        PessoaAcessoTipo tipo,
        string cpf,
        CancellationToken cancellationToken = default)
    {
        var cpfValue = CriarCpf(cpf);
        return tipo switch
        {
            PessoaAcessoTipo.Aluno => await RegistrarAcessoAlunoAsync(cpfValue, cancellationToken),
            PessoaAcessoTipo.Colaborador => await RegistrarAcessoColaboradorAsync(cpfValue, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(tipo), tipo, "Tipo de pessoa inválido.")
        };
    }

    private async Task<AcessoPessoaDto?> ObterAlunoAsync(Cpf cpf, CancellationToken cancellationToken)
    {
        var aluno = await _alunoRepositoryFactory().ObterPorCpf(cpf, cancellationToken);
        if (aluno is null)
        {
            return null;
        }

        var acessoRepository = _acessoAlunoRepositoryFactory();
        bool estaNaAcademia = await acessoRepository.EstaNaAcademia(aluno.Id, cancellationToken);
        var ultimoAcesso = await acessoRepository.ObterUltimoAcesso(aluno.Id, cancellationToken);
        return new AcessoPessoaDto
        {
            Id = aluno.Id,
            Nome = aluno.Nome,
            Cpf = aluno.Cpf.Valor,
            Tipo = PessoaAcessoTipo.Aluno,
            EstaNaAcademia = estaNaAcademia,
            UltimoAcesso = ultimoAcesso?.DataHora
        };
    }

    private async Task<AcessoPessoaDto?> ObterColaboradorAsync(Cpf cpf, CancellationToken cancellationToken)
    {
        var colaborador = await _colaboradorRepositoryFactory().ObterPorCpf(cpf, cancellationToken);
        if (colaborador is null)
        {
            return null;
        }

        var acessoRepository = _acessoColaboradorRepositoryFactory();
        bool estaNaAcademia = await acessoRepository.EstaNaAcademia(colaborador.Id, cancellationToken);
        var ultimoAcesso = await acessoRepository.ObterUltimoAcesso(colaborador.Id, cancellationToken);
        return new AcessoPessoaDto
        {
            Id = colaborador.Id,
            Nome = colaborador.Nome,
            Cpf = colaborador.Cpf.Valor,
            Tipo = PessoaAcessoTipo.Colaborador,
            EstaNaAcademia = estaNaAcademia,
            UltimoAcesso = ultimoAcesso?.DataHora
        };
    }

    private async Task<AcessoPessoaDto> RegistrarAcessoAlunoAsync(Cpf cpf, CancellationToken cancellationToken)
    {
        var aluno = await _alunoRepositoryFactory().ObterPorCpf(cpf, cancellationToken)
            ?? throw new KeyNotFoundException("Aluno não encontrado para o CPF informado.");
        var acessoRepository = _acessoAlunoRepositoryFactory();
        bool estaNaAcademia = await acessoRepository.EstaNaAcademia(aluno.Id, cancellationToken);

        if (!estaNaAcademia && !await _matriculaRepositoryFactory().PossuiMatriculaAtiva(aluno.Id, cancellationToken))
        {
            throw new InvalidOperationException("O aluno não possui matrícula ativa e não pode registrar entrada.");
        }

        DateTime agora = _timeProvider.GetLocalNow().DateTime;
        var resultado = AcessoAluno.Criar(0, aluno, agora);
        if (resultado.IsFailure)
        {
            throw new InvalidOperationException(
                $"Não foi possível registrar o acesso: {string.Join(", ", resultado.Notifications.Select(n => n.Mensagem))}");
        }

        await acessoRepository.Adicionar(resultado.Value!, cancellationToken);
        return new AcessoPessoaDto
        {
            Id = aluno.Id,
            Nome = aluno.Nome,
            Cpf = aluno.Cpf.Valor,
            Tipo = PessoaAcessoTipo.Aluno,
            EstaNaAcademia = !estaNaAcademia,
            UltimoAcesso = agora
        };
    }

    private async Task<AcessoPessoaDto> RegistrarAcessoColaboradorAsync(Cpf cpf, CancellationToken cancellationToken)
    {
        var colaborador = await _colaboradorRepositoryFactory().ObterPorCpf(cpf, cancellationToken)
            ?? throw new KeyNotFoundException("Colaborador não encontrado para o CPF informado.");

        bool estaNaAcademia = await _acessoColaboradorRepositoryFactory()
            .EstaNaAcademia(colaborador.Id, cancellationToken);
        DateTime agora = _timeProvider.GetLocalNow().DateTime;
        var resultado = AcessoColaborador.Criar(0, colaborador, agora);
        if (resultado.IsFailure)
        {
            throw new InvalidOperationException(
                $"Não foi possível registrar o acesso: {string.Join(", ", resultado.Notifications.Select(n => n.Mensagem))}");
        }

        await _acessoColaboradorRepositoryFactory().Adicionar(resultado.Value!, cancellationToken);
        return new AcessoPessoaDto
        {
            Id = colaborador.Id,
            Nome = colaborador.Nome,
            Cpf = colaborador.Cpf.Valor,
            Tipo = PessoaAcessoTipo.Colaborador,
            EstaNaAcademia = !estaNaAcademia,
            UltimoAcesso = agora
        };
    }

    private static Cpf CriarCpf(string cpf)
    {
        if (string.IsNullOrWhiteSpace(cpf))
        {
            throw new ArgumentException("CPF não pode ser vazio.", nameof(cpf));
        }

        var resultado = Cpf.Criar(cpf);
        if (resultado.IsFailure)
        {
            throw new ArgumentException(
                $"CPF inválido: {string.Join(", ", resultado.Notifications.Select(n => n.Mensagem))}",
                nameof(cpf));
        }

        return resultado.Value!;
    }

    private static bool Corresponde(string nome, string cpf, string busca, string buscaNumerica)
    {
        return nome.Contains(busca, StringComparison.CurrentCultureIgnoreCase) ||
               (buscaNumerica.Length > 0 && ObterDigitos(cpf).Contains(buscaNumerica, StringComparison.Ordinal));
    }

    private static string ObterDigitos(string valor) =>
        new(valor.Where(char.IsDigit).ToArray());
}
