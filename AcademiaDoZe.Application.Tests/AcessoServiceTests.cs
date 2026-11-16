using AcademiaDoZe.Application.Enums;
using AcademiaDoZe.Application.Services;
using AcademiaDoZe.Domain.Entities;
using AcademiaDoZe.Domain.Enums;
using AcademiaDoZe.Domain.Repositories;
using AcademiaDoZe.Domain.ValueObjects;
using System.Reflection;

namespace AcademiaDoZe.Application.Tests;

public class AcessoServiceTests
{
    private const string CpfValido = "529.982.247-25";

    [Fact]
    public async Task Aluno_sem_matricula_ativa_nao_pode_registrar_entrada()
    {
        int consultasMatricula = 0;
        AcessoAluno? acessoAdicionado = null;
        var service = CriarService(
            possuiMatriculaAtiva: false,
            estaNaAcademia: false,
            aluno =>
            {
                acessoAdicionado = aluno;
                return aluno;
            },
            () => consultasMatricula++,
            CriarRelogio());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.RegistrarProximoAcessoAsync(PessoaAcessoTipo.Aluno, CpfValido));

        Assert.Equal(1, consultasMatricula);
        Assert.Null(acessoAdicionado);
    }

    [Fact]
    public async Task Aluno_com_matricula_ativa_pode_registrar_entrada()
    {
        AcessoAluno? acessoAdicionado = null;
        var service = CriarService(
            possuiMatriculaAtiva: true,
            estaNaAcademia: false,
            aluno =>
            {
                acessoAdicionado = aluno;
                return aluno;
            },
            () => { },
            CriarRelogio());

        var resultado = await service.RegistrarProximoAcessoAsync(PessoaAcessoTipo.Aluno, CpfValido);

        Assert.NotNull(acessoAdicionado);
        Assert.True(resultado.EstaNaAcademia);
        Assert.Equal("Registrar saída", resultado.ProximaAcao);
        Assert.Equal(new DateTime(2026, 11, 15, 10, 0, 0), resultado.UltimoAcesso);
    }

    [Fact]
    public async Task Aluno_que_ja_esta_na_academia_pode_registrar_saida_sem_matricula_ativa()
    {
        AcessoAluno? acessoAdicionado = null;
        int consultasMatricula = 0;
        var service = CriarService(
            possuiMatriculaAtiva: false,
            estaNaAcademia: true,
            aluno =>
            {
                acessoAdicionado = aluno;
                return aluno;
            },
            () => consultasMatricula++,
            CriarRelogio());

        var resultado = await service.RegistrarProximoAcessoAsync(PessoaAcessoTipo.Aluno, CpfValido);

        Assert.NotNull(acessoAdicionado);
        Assert.Equal(0, consultasMatricula);
        Assert.False(resultado.EstaNaAcademia);
        Assert.Equal("Registrar entrada", resultado.ProximaAcao);
    }

    [Fact]
    public async Task Busca_por_parte_do_cpf_retorna_pessoa_com_situacao_atual()
    {
        var aluno = CriarAluno();
        var alunoRepository = CriarProxy<IAlunoRepository>((nome, _) => nome switch
        {
            "ObterTodos" => Task.FromResult<IEnumerable<Aluno>>([aluno]),
            _ => throw new NotSupportedException($"Método não esperado: {nome}")
        });
        var acessoAlunoRepository = CriarProxy<IAcessoAlunoRepository>((nome, _) => nome switch
        {
            "EstaNaAcademia" => Task.FromResult(true),
            "ObterUltimoAcesso" => Task.FromResult<AcessoAluno?>(null),
            _ => throw new NotSupportedException($"Método não esperado: {nome}")
        });
        var service = new AcessoService(
            () => alunoRepository,
            () => throw new NotSupportedException(),
            () => acessoAlunoRepository,
            () => throw new NotSupportedException(),
            () => throw new NotSupportedException());

        var resultado = await service.BuscarPessoasAsync(PessoaAcessoTipo.Aluno, "982247");

        var pessoa = Assert.Single(resultado);
        Assert.Equal("Aluno Teste", pessoa.Nome);
        Assert.True(pessoa.EstaNaAcademia);
    }

    [Fact]
    public async Task Acessos_de_hoje_sao_ordenados_e_classificados_pela_situacao_atual()
    {
        var aluno = CriarAluno();
        var acessoAnterior = AcessoAluno.Criar(10, aluno, new DateTime(2026, 11, 15, 8, 0, 0)).Value!;
        var acessoMaisRecente = AcessoAluno.Criar(11, aluno, new DateTime(2026, 11, 15, 12, 0, 0)).Value!;
        var acessoAlunoRepository = CriarProxy<IAcessoAlunoRepository>((nome, _) => nome switch
        {
            "ObterAcessosPorAlunoPeriodo" => Task.FromResult<IEnumerable<AcessoAluno>>(
                [acessoAnterior, acessoMaisRecente]),
            "EstaNaAcademia" => Task.FromResult(true),
            _ => throw new NotSupportedException($"Método não esperado: {nome}")
        });
        var acessoColaboradorRepository = CriarProxy<IAcessoColaboradorRepository>((nome, _) => nome switch
        {
            "ObterAcessosPorColaboradorPeriodo" => Task.FromResult<IEnumerable<AcessoColaborador>>([]),
            _ => throw new NotSupportedException($"Método não esperado: {nome}")
        });
        var service = new AcessoService(
            () => throw new NotSupportedException(),
            () => throw new NotSupportedException(),
            () => acessoAlunoRepository,
            () => acessoColaboradorRepository,
            () => throw new NotSupportedException(),
            CriarRelogio());

        var resultado = await service.ObterAcessosDeHojeAsync();

        Assert.Equal(2, resultado.Count);
        Assert.True(resultado[0].Entrada);
        Assert.Equal(new DateTime(2026, 11, 15, 12, 0, 0), resultado[0].DataHora);
        Assert.False(resultado[1].Entrada);
    }

    private static AcessoService CriarService(
        bool possuiMatriculaAtiva,
        bool estaNaAcademia,
        Func<AcessoAluno, AcessoAluno> aoAdicionarAcesso,
        Action aoConsultarMatricula,
        TimeProvider timeProvider)
    {
        var aluno = CriarAluno();

        var alunoRepository = CriarProxy<IAlunoRepository>((nome, _) => nome switch
        {
            "ObterPorCpf" => Task.FromResult<Aluno?>(aluno),
            _ => throw new NotSupportedException($"Método não esperado: {nome}")
        });
        var colaboradorRepository = CriarProxy<IColaboradorRepository>((nome, _) =>
            throw new NotSupportedException($"Método não esperado: {nome}"));
        var acessoAlunoRepository = CriarProxy<IAcessoAlunoRepository>((nome, argumentos) => nome switch
        {
            "EstaNaAcademia" => Task.FromResult(estaNaAcademia),
            "Adicionar" => Task.FromResult(aoAdicionarAcesso((AcessoAluno)argumentos[0]!)),
            _ => throw new NotSupportedException($"Método não esperado: {nome}")
        });
        var acessoColaboradorRepository = CriarProxy<IAcessoColaboradorRepository>((nome, _) =>
            throw new NotSupportedException($"Método não esperado: {nome}"));
        var matriculaRepository = CriarProxy<IMatriculaRepository>((nome, _) =>
        {
            if (nome == "PossuiMatriculaAtiva")
            {
                aoConsultarMatricula();
                return Task.FromResult(possuiMatriculaAtiva);
            }

            throw new NotSupportedException($"Método não esperado: {nome}");
        });

        return new AcessoService(
            () => alunoRepository,
            () => colaboradorRepository,
            () => acessoAlunoRepository,
            () => acessoColaboradorRepository,
            () => matriculaRepository,
            timeProvider);
    }

    private static Aluno CriarAluno()
    {
        var logradouro = Logradouro.Criar(1, "12345-678", "Rua Teste", "Centro", "São Paulo", "SP", "Brasil").Value!;
        return Aluno.Criar(
            1,
            "Aluno Teste",
            CpfValido,
            DateOnly.FromDateTime(DateTime.Today.AddYears(-20)),
            "(11) 91234-5678",
            "aluno@teste.com",
            logradouro,
            "123",
            string.Empty,
            "Abcdef",
            Arquivo.Criar([1, 2, 3]).Value!).Value!;
    }

    private static T CriarProxy<T>(Func<string, object?[], object?> handler)
        where T : class
    {
        var proxy = DispatchProxy.Create<T, RepositoryProxy<T>>();
        ((RepositoryProxy<T>)(object)proxy).Handler = handler;
        return proxy;
    }

    private static TimeProvider CriarRelogio()
    {
        return new FixedTimeProvider(new DateTime(2026, 11, 15, 10, 0, 0));
    }

    public class RepositoryProxy<T> : DispatchProxy where T : class
    {
        public Func<string, object?[], object?> Handler { get; set; } =
            (_, _) => throw new InvalidOperationException("Handler do proxy não configurado.");

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            return Handler(targetMethod!.Name, args ?? []);
        }
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _now;

        public FixedTimeProvider(DateTime now)
        {
            _now = new DateTimeOffset(now, TimeZoneInfo.Local.GetUtcOffset(now));
        }

        public override DateTimeOffset GetUtcNow() => _now.ToUniversalTime();
    }
}
