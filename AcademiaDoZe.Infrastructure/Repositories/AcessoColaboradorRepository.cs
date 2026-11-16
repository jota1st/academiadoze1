using AcademiaDoZe.Domain.Entities;
using AcademiaDoZe.Domain.Repositories;
using AcademiaDoZe.Infrastructure.Data;
using AcademiaDoZe.Infrastructure.Exceptions;
using System.Data.Common;

namespace AcademiaDoZe.Infrastructure.Repositories;

public class AcessoColaboradorRepository : AcessoRepositoryBase<AcessoColaborador>, IAcessoColaboradorRepository
{
    private const int TipoColaborador = ColaboradorTipo;

    public AcessoColaboradorRepository(string connectionString, DatabaseType databaseType)
        : base(connectionString, databaseType)
    {
    }

    protected override int PessoaTipo => TipoColaborador;

    protected override string BaseSelectQuery => """
        SELECT
            ac.id_acesso, ac.data_hora,
            c.id_colaborador, c.cpf, c.nome AS colaborador_nome, c.nascimento, c.telefone, c.email,
            c.logradouro_id, c.numero, c.complemento, c.senha, c.foto, c.admissao, c.tipo, c.vinculo,
            l.id_logradouro, l.cep, l.nome AS logradouro_nome, l.bairro, l.cidade, l.estado, l.pais
        FROM tb_acesso ac
        INNER JOIN tb_colaborador c ON ac.pessoa_id = c.id_colaborador
        INNER JOIN tb_logradouro l ON c.logradouro_id = l.id_logradouro
        """;

    protected override int ObterPessoaId(AcessoColaborador entity) => entity.Colaborador.Id;

    protected override DateTime ObterDataHora(AcessoColaborador entity) => entity.DataHora;

    protected override AcessoColaborador Map(DbDataReader reader)
    {
        int id = reader.GetInt32Value("id_acesso");
        var colaborador = ColaboradorRepository.Map(reader, "colaborador_nome");
        var result = AcessoColaborador.Criar(id, colaborador, reader.GetDateTimeValue("data_hora"));
        if (result.IsFailure)
        {
            throw new InfrastructureException(
                "ERRO_DOMINIO_MAPEAMENTO_ACESSO_COLABORADOR",
                $"Erro de domínio ao mapear acesso ID {id}: {string.Join(", ", result.Notifications.Select(n => n.Mensagem))}");
        }
        return result.Value!;
    }

    public Task<IEnumerable<AcessoColaborador>> ObterAcessosPorColaboradorPeriodo(
        int? colaboradorId = null,
        DateOnly? inicio = null,
        DateOnly? fim = null,
        CancellationToken cancellationToken = default)
    {
        return ObterPorPeriodoAsync(colaboradorId, inicio, fim, cancellationToken);
    }

    public Task<AcessoColaborador?> ObterUltimoAcesso(int colaboradorId, CancellationToken cancellationToken = default)
    {
        return ObterUltimoAcessoAsync(colaboradorId, cancellationToken);
    }

    public Task<bool> EstaNaAcademia(int colaboradorId, CancellationToken cancellationToken = default)
    {
        return PessoaEstaNaAcademiaAsync(colaboradorId, cancellationToken);
    }

    public async Task<TimeSpan> ObterHorasTrabalhadasNoDia(
        int colaboradorId,
        DateOnly data,
        CancellationToken cancellationToken = default)
    {
        var acessos = await ObterPorPeriodoAsync(colaboradorId, data, data, cancellationToken);
        var eventos = acessos
            .OrderBy(acesso => acesso.DataHora)
            .ThenBy(acesso => acesso.Id)
            .ToList();
        long ticks = 0;

        for (int indice = 0; indice + 1 < eventos.Count; indice += 2)
        {
            ticks += (eventos[indice + 1].DataHora - eventos[indice].DataHora).Ticks;
        }

        return TimeSpan.FromTicks(ticks);
    }
}
