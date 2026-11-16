using AcademiaDoZe.Domain.Common;
using AcademiaDoZe.Domain.Entities;
using AcademiaDoZe.Domain.Repositories;
using AcademiaDoZe.Infrastructure.Data;
using AcademiaDoZe.Infrastructure.Exceptions;
using System.Data;
using System.Data.Common;

namespace AcademiaDoZe.Infrastructure.Repositories;

public abstract class AcessoRepositoryBase<TEntity> : BaseRepository, IRepository<TEntity>
    where TEntity : Entity, IAggregateRoot
{
    protected const int AlunoTipo = 0;
    protected const int ColaboradorTipo = 1;

    protected AcessoRepositoryBase(string connectionString, DatabaseType databaseType)
        : base(connectionString, databaseType)
    {
    }

    protected abstract int PessoaTipo { get; }
    protected abstract string BaseSelectQuery { get; }
    protected abstract int ObterPessoaId(TEntity entity);
    protected abstract DateTime ObterDataHora(TEntity entity);
    protected abstract TEntity Map(DbDataReader reader);

    public async Task<TEntity?> ObterPorId(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            string query = $"{BaseSelectQuery} WHERE ac.pessoa_tipo = @Tipo AND ac.id_acesso = @Id";
            await using var command = await CreateCommandAsync(query, cancellationToken);
            command.AddParameter("@Tipo", PessoaTipo, DbType.Int32);
            command.AddParameter("@Id", id, DbType.Int32);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            return await reader.ReadAsync(cancellationToken) ? Map(reader) : null;
        }
        catch (DbException ex)
        {
            throw new InfrastructureException("ERRO_OBTER_ACESSO_POR_ID", $"Erro ao obter acesso por ID {id}: {ex.Message}", ex);
        }
    }

    public async Task<IEnumerable<TEntity>> ObterTodos(CancellationToken cancellationToken = default)
    {
        try
        {
            string query = $"{BaseSelectQuery} WHERE ac.pessoa_tipo = @Tipo ORDER BY ac.data_hora, ac.id_acesso";
            await using var command = await CreateCommandAsync(query, cancellationToken);
            command.AddParameter("@Tipo", PessoaTipo, DbType.Int32);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var acessos = new List<TEntity>();
            while (await reader.ReadAsync(cancellationToken))
            {
                acessos.Add(Map(reader));
            }
            return acessos;
        }
        catch (DbException ex)
        {
            throw new InfrastructureException("ERRO_OBTER_ACESSOS", $"Erro ao obter registros de acesso: {ex.Message}", ex);
        }
    }

    public async Task<TEntity> Adicionar(TEntity entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        try
        {
            string query = FormatInsertQuery(
                "INSERT INTO tb_acesso (pessoa_tipo, pessoa_id, data_hora) VALUES (@Tipo, @PessoaId, @DataHora)");
            await using var command = await CreateCommandAsync(query, cancellationToken);
            command.AddParameter("@Tipo", PessoaTipo, DbType.Int32);
            command.AddParameter("@PessoaId", ObterPessoaId(entity), DbType.Int32);
            command.AddParameter("@DataHora", ObterDataHora(entity), DbType.DateTime);
            int id = await command.ExecuteScalarIdAsync(
                "ERRO_REGISTRAR_ACESSO",
                "Falha ao obter o ID do acesso registrado.",
                cancellationToken);
            SetEntityId(entity, id);
            return entity;
        }
        catch (DbException ex)
        {
            throw new InfrastructureException("ERRO_REGISTRAR_ACESSO", $"Erro ao registrar acesso: {ex.Message}", ex);
        }
    }

    public async Task<TEntity> Atualizar(TEntity entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        try
        {
            const string query = """
                UPDATE tb_acesso
                SET pessoa_id = @PessoaId, data_hora = @DataHora
                WHERE id_acesso = @Id AND pessoa_tipo = @Tipo
                """;
            await using var command = await CreateCommandAsync(query, cancellationToken);
            command.AddParameter("@PessoaId", ObterPessoaId(entity), DbType.Int32);
            command.AddParameter("@DataHora", ObterDataHora(entity), DbType.DateTime);
            command.AddParameter("@Id", entity.Id, DbType.Int32);
            command.AddParameter("@Tipo", PessoaTipo, DbType.Int32);
            if (await command.ExecuteNonQueryAsync(cancellationToken) == 0)
            {
                throw new InfrastructureException("ACESSO_NAO_ENCONTRADO", $"Acesso com ID {entity.Id} não encontrado.");
            }
            return entity;
        }
        catch (DbException ex)
        {
            throw new InfrastructureException("ERRO_ATUALIZAR_ACESSO", $"Erro ao atualizar acesso ID {entity.Id}: {ex.Message}", ex);
        }
    }

    public async Task<bool> Remover(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            const string query = "DELETE FROM tb_acesso WHERE id_acesso = @Id AND pessoa_tipo = @Tipo";
            await using var command = await CreateCommandAsync(query, cancellationToken);
            command.AddParameter("@Id", id, DbType.Int32);
            command.AddParameter("@Tipo", PessoaTipo, DbType.Int32);
            return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
        }
        catch (DbException ex)
        {
            throw new InfrastructureException("ERRO_REMOVER_ACESSO", $"Erro ao remover acesso ID {id}: {ex.Message}", ex);
        }
    }

    protected async Task<IEnumerable<TEntity>> ObterPorPeriodoAsync(
        int? pessoaId,
        DateOnly? inicio,
        DateOnly? fim,
        CancellationToken cancellationToken)
    {
        if (inicio.HasValue && fim.HasValue && inicio.Value > fim.Value)
        {
            throw new ArgumentException("A data inicial não pode ser posterior à data final.", nameof(inicio));
        }

        var conditions = new List<string> { "ac.pessoa_tipo = @Tipo" };
        if (pessoaId.HasValue)
        {
            conditions.Add("ac.pessoa_id = @PessoaId");
        }
        if (inicio.HasValue)
        {
            conditions.Add("ac.data_hora >= @Inicio");
        }
        if (fim.HasValue && fim.Value < DateOnly.MaxValue)
        {
            conditions.Add("ac.data_hora < @FimExclusivo");
        }

        string query = $"{BaseSelectQuery} WHERE {string.Join(" AND ", conditions)} ORDER BY ac.data_hora, ac.id_acesso";
        await using var command = await CreateCommandAsync(query, cancellationToken);
        command.AddParameter("@Tipo", PessoaTipo, DbType.Int32);
        if (pessoaId.HasValue)
        {
            command.AddParameter("@PessoaId", pessoaId.Value, DbType.Int32);
        }
        if (inicio.HasValue)
        {
            command.AddParameter("@Inicio", inicio.Value.ToDateTime(TimeOnly.MinValue), DbType.DateTime);
        }
        if (fim.HasValue && fim.Value < DateOnly.MaxValue)
        {
            command.AddParameter("@FimExclusivo", fim.Value.AddDays(1).ToDateTime(TimeOnly.MinValue), DbType.DateTime);
        }

        try
        {
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var acessos = new List<TEntity>();
            while (await reader.ReadAsync(cancellationToken))
            {
                acessos.Add(Map(reader));
            }
            return acessos;
        }
        catch (DbException ex)
        {
            throw new InfrastructureException("ERRO_OBTER_ACESSOS_POR_PERIODO", $"Erro ao consultar acessos por período: {ex.Message}", ex);
        }
    }

    protected async Task<TEntity?> ObterUltimoAcessoAsync(int pessoaId, CancellationToken cancellationToken)
    {
        try
        {
            string query = $"{BaseSelectQuery} WHERE ac.pessoa_tipo = @Tipo AND ac.pessoa_id = @PessoaId ORDER BY ac.data_hora DESC, ac.id_acesso DESC";
            await using var command = await CreateCommandAsync(query, cancellationToken);
            command.AddParameter("@Tipo", PessoaTipo, DbType.Int32);
            command.AddParameter("@PessoaId", pessoaId, DbType.Int32);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            return await reader.ReadAsync(cancellationToken) ? Map(reader) : null;
        }
        catch (DbException ex)
        {
            throw new InfrastructureException("ERRO_OBTER_ULTIMO_ACESSO", $"Erro ao obter o último acesso da pessoa {pessoaId}: {ex.Message}", ex);
        }
    }

    protected async Task<bool> PessoaEstaNaAcademiaAsync(int pessoaId, CancellationToken cancellationToken)
    {
        try
        {
            const string query = "SELECT COUNT(1) FROM tb_acesso WHERE pessoa_tipo = @Tipo AND pessoa_id = @PessoaId";
            await using var command = await CreateCommandAsync(query, cancellationToken);
            command.AddParameter("@Tipo", PessoaTipo, DbType.Int32);
            command.AddParameter("@PessoaId", pessoaId, DbType.Int32);
            int totalAcessos = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
            return totalAcessos % 2 == 1;
        }
        catch (DbException ex)
        {
            throw new InfrastructureException("ERRO_VERIFICAR_PRESENCA", $"Erro ao verificar presença da pessoa {pessoaId}: {ex.Message}", ex);
        }
    }

    private static void SetEntityId(TEntity entity, int id)
    {
        var idProperty = typeof(Entity).GetProperty(nameof(Entity.Id))
            ?? throw new InvalidOperationException("A entidade de acesso não possui a propriedade Id.");
        idProperty.SetValue(entity, id);
    }
}
