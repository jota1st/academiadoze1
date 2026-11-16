using AcademiaDoZe.Domain.Entities;
using AcademiaDoZe.Domain.Repositories;
using AcademiaDoZe.Infrastructure.Data;
using AcademiaDoZe.Infrastructure.Exceptions;
using System.Data.Common;

namespace AcademiaDoZe.Infrastructure.Repositories;

public class AcessoAlunoRepository : AcessoRepositoryBase<AcessoAluno>, IAcessoAlunoRepository
{
    private const int TipoAluno = AlunoTipo;

    public AcessoAlunoRepository(string connectionString, DatabaseType databaseType)
        : base(connectionString, databaseType)
    {
    }

    protected override int PessoaTipo => TipoAluno;

    protected override string BaseSelectQuery => """
        SELECT
            ac.id_acesso, ac.data_hora,
            a.id_aluno, a.cpf, a.nome AS aluno_nome, a.nascimento, a.telefone, a.email,
            a.logradouro_id, a.numero, a.complemento, a.senha, a.foto,
            l.id_logradouro, l.cep, l.nome AS logradouro_nome, l.bairro, l.cidade, l.estado, l.pais
        FROM tb_acesso ac
        INNER JOIN tb_aluno a ON ac.pessoa_id = a.id_aluno
        INNER JOIN tb_logradouro l ON a.logradouro_id = l.id_logradouro
        """;

    protected override int ObterPessoaId(AcessoAluno entity) => entity.Aluno.Id;

    protected override DateTime ObterDataHora(AcessoAluno entity) => entity.DataHora;

    protected override AcessoAluno Map(DbDataReader reader)
    {
        int id = reader.GetInt32Value("id_acesso");
        var aluno = AlunoRepository.Map(reader, "aluno_nome");
        var result = AcessoAluno.Criar(id, aluno, reader.GetDateTimeValue("data_hora"));
        if (result.IsFailure)
        {
            throw new InfrastructureException(
                "ERRO_DOMINIO_MAPEAMENTO_ACESSO_ALUNO",
                $"Erro de domínio ao mapear acesso ID {id}: {string.Join(", ", result.Notifications.Select(n => n.Mensagem))}");
        }
        return result.Value!;
    }

    public Task<IEnumerable<AcessoAluno>> ObterAcessosPorAlunoPeriodo(
        int? alunoId = null,
        DateOnly? inicio = null,
        DateOnly? fim = null,
        CancellationToken cancellationToken = default)
    {
        return ObterPorPeriodoAsync(alunoId, inicio, fim, cancellationToken);
    }

    public Task<AcessoAluno?> ObterUltimoAcesso(int alunoId, CancellationToken cancellationToken = default)
    {
        return ObterUltimoAcessoAsync(alunoId, cancellationToken);
    }

    public Task<bool> EstaNaAcademia(int alunoId, CancellationToken cancellationToken = default)
    {
        return PessoaEstaNaAcademiaAsync(alunoId, cancellationToken);
    }

    public async Task<Dictionary<TimeOnly, int>> ObterHorarioMaisProcuradoPorMes(
        int mes,
        CancellationToken cancellationToken = default)
    {
        ValidarMes(mes);
        var acessos = await ObterTodos(cancellationToken);
        var entradas = acessos
            .GroupBy(acesso => acesso.Aluno.Id)
            .SelectMany(grupo => grupo
                .OrderBy(acesso => acesso.DataHora)
                .ThenBy(acesso => acesso.Id)
                .Where((_, indice) => indice % 2 == 0))
            .Where(acesso => acesso.DataHora.Month == mes);

        return entradas
            .GroupBy(acesso => new TimeOnly(acesso.DataHora.Hour, 0))
            .ToDictionary(grupo => grupo.Key, grupo => grupo.Count());
    }

    public async Task<Dictionary<int, TimeSpan>> ObterPermanenciaMediaPorMes(
        int mes,
        CancellationToken cancellationToken = default)
    {
        ValidarMes(mes);
        var acessos = await ObterTodos(cancellationToken);
        var permanencias = ObterVisitasCompletas(acessos)
            .Where(visita => visita.Entrada.Month == mes)
            .Select(visita => (double)(visita.Saida - visita.Entrada).Ticks)
            .ToList();

        if (permanencias.Count == 0)
        {
            return [];
        }

        return new Dictionary<int, TimeSpan>
        {
            [mes] = TimeSpan.FromTicks((long)permanencias.Average())
        };
    }

    public async Task<IEnumerable<Aluno>> ObterAlunosSemAcessoNosUltimosDias(
        int dias,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(dias);

        var acessos = await ObterTodos(cancellationToken);
        var ultimoAcessoPorAluno = acessos
            .GroupBy(acesso => acesso.Aluno.Id)
            .ToDictionary(grupo => grupo.Key, grupo => grupo.Max(acesso => acesso.DataHora));
        DateTime limite = DateTime.Now.AddDays(-dias);

        await using var alunoRepository = new AlunoRepository(_connectionString, _databaseType);
        var alunos = await alunoRepository.ObterTodos(cancellationToken);
        return alunos.Where(aluno =>
            !ultimoAcessoPorAluno.TryGetValue(aluno.Id, out DateTime ultimoAcesso) ||
            ultimoAcesso < limite);
    }

    private static IEnumerable<(DateTime Entrada, DateTime Saida)> ObterVisitasCompletas(
        IEnumerable<AcessoAluno> acessos)
    {
        foreach (var grupo in acessos.GroupBy(acesso => acesso.Aluno.Id))
        {
            var ordenados = grupo
                .OrderBy(acesso => acesso.DataHora)
                .ThenBy(acesso => acesso.Id)
                .ToList();

            for (int indice = 0; indice + 1 < ordenados.Count; indice += 2)
            {
                yield return (ordenados[indice].DataHora, ordenados[indice + 1].DataHora);
            }
        }
    }

    private static void ValidarMes(int mes)
    {
        if (mes is < 1 or > 12)
        {
            throw new ArgumentOutOfRangeException(nameof(mes), "O mês deve estar entre 1 e 12.");
        }
    }
}
