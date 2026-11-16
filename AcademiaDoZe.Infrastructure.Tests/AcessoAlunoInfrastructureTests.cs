using AcademiaDoZe.Domain.Entities;
using AcademiaDoZe.Domain.Enums;
using AcademiaDoZe.Domain.ValueObjects;
using AcademiaDoZe.Infrastructure.Data;
using AcademiaDoZe.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;

namespace AcademiaDoZe.Infrastructure.Tests;

public class AcessoAlunoInfrastructureTests
{
    [Fact]
    public async Task Registro_de_entrada_e_saida_alterna_presenca_e_consulta_historico()
    {
        string databasePath = Path.Combine(Path.GetTempPath(), $"academiadoze-{Guid.NewGuid():N}.db");
        string connectionString = $"Data Source={databasePath};Cache=Shared";

        try
        {
            await using var logradouroRepository = new LogradouroRepository(connectionString, DatabaseType.Sqlite);
            await using var alunoRepository = new AlunoRepository(connectionString, DatabaseType.Sqlite);
            await using var acessoRepository = new AcessoAlunoRepository(connectionString, DatabaseType.Sqlite);

            var logradouro = await logradouroRepository.Adicionar(
                Logradouro.Criar(0, "12345-678", "Rua Teste", "Centro", "São Paulo", "SP", "Brasil").Value!);
            var aluno = await alunoRepository.Adicionar(CriarAluno(logradouro));

            Assert.False(await acessoRepository.EstaNaAcademia(aluno.Id));

            DateTime entrada = DateTime.Today.AddHours(9);
            await acessoRepository.Adicionar(AcessoAluno.Criar(0, aluno, entrada).Value!);

            Assert.True(await acessoRepository.EstaNaAcademia(aluno.Id));
            Assert.Equal(entrada, (await acessoRepository.ObterUltimoAcesso(aluno.Id))!.DataHora);

            DateTime saida = DateTime.Today.AddHours(11);
            await acessoRepository.Adicionar(AcessoAluno.Criar(0, aluno, saida).Value!);

            Assert.False(await acessoRepository.EstaNaAcademia(aluno.Id));
            var acessos = await acessoRepository.ObterAcessosPorAlunoPeriodo(
                aluno.Id,
                DateOnly.FromDateTime(DateTime.Today),
                DateOnly.FromDateTime(DateTime.Today));
            Assert.Equal(2, acessos.Count());
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(databasePath))
            {
                File.Delete(databasePath);
            }
        }
    }

    [Fact]
    public async Task Matricula_com_inicio_futuro_nao_permite_entrada_ate_a_data_de_inicio()
    {
        string databasePath = Path.Combine(Path.GetTempPath(), $"academiadoze-{Guid.NewGuid():N}.db");
        string connectionString = $"Data Source={databasePath};Cache=Shared";

        try
        {
            await using var logradouroRepository = new LogradouroRepository(connectionString, DatabaseType.Sqlite);
            await using var alunoRepository = new AlunoRepository(connectionString, DatabaseType.Sqlite);
            await using var matriculaRepository = new MatriculaRepository(connectionString, DatabaseType.Sqlite);

            var logradouro = await logradouroRepository.Adicionar(
                Logradouro.Criar(0, "12345-678", "Rua Teste", "Centro", "São Paulo", "SP", "Brasil").Value!);
            var aluno = await alunoRepository.Adicionar(CriarAluno(logradouro));
            var matriculaFutura = Matricula.Criar(
                0,
                aluno,
                MatriculaPlano.Mensal,
                DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
                "Condicionamento físico",
                MatriculaRestricoes.None,
                null).Value!;

            await matriculaRepository.Adicionar(matriculaFutura);

            Assert.False(await matriculaRepository.PossuiMatriculaAtiva(aluno.Id));

            var matriculaVigente = Matricula.Criar(
                0,
                aluno,
                MatriculaPlano.Mensal,
                DateOnly.FromDateTime(DateTime.Today),
                "Condicionamento físico",
                MatriculaRestricoes.None,
                null).Value!;
            await matriculaRepository.Adicionar(matriculaVigente);

            Assert.True(await matriculaRepository.PossuiMatriculaAtiva(aluno.Id));
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(databasePath))
            {
                File.Delete(databasePath);
            }
        }
    }

    private static Aluno CriarAluno(Logradouro logradouro)
    {
        return Aluno.Criar(
            0,
            "Aluno Teste",
            "529.982.247-25",
            DateOnly.FromDateTime(DateTime.Today.AddYears(-20)),
            "(11) 91234-5678",
            "aluno@teste.com",
            logradouro,
            "123",
            string.Empty,
            "Abcdef",
            Arquivo.Criar([1, 2, 3]).Value!).Value!;
    }
}
