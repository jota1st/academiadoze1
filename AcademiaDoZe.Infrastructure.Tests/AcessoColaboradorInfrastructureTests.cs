using AcademiaDoZe.Domain.Entities;
using AcademiaDoZe.Domain.Enums;
using AcademiaDoZe.Domain.ValueObjects;
using AcademiaDoZe.Infrastructure.Data;
using AcademiaDoZe.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;

namespace AcademiaDoZe.Infrastructure.Tests;

public class AcessoColaboradorInfrastructureTests
{
    [Fact]
    public async Task Registro_de_entrada_e_saida_calcula_horas_trabalhadas()
    {
        string databasePath = Path.Combine(Path.GetTempPath(), $"academiadoze-{Guid.NewGuid():N}.db");
        string connectionString = $"Data Source={databasePath};Cache=Shared";

        try
        {
            await using var logradouroRepository = new LogradouroRepository(connectionString, DatabaseType.Sqlite);
            await using var colaboradorRepository = new ColaboradorRepository(connectionString, DatabaseType.Sqlite);
            await using var acessoRepository = new AcessoColaboradorRepository(connectionString, DatabaseType.Sqlite);

            var logradouro = await logradouroRepository.Adicionar(
                Logradouro.Criar(0, "12345-678", "Rua Teste", "Centro", "São Paulo", "SP", "Brasil").Value!);
            var colaborador = await colaboradorRepository.Adicionar(CriarColaborador(logradouro));

            Assert.False(await acessoRepository.EstaNaAcademia(colaborador.Id));

            DateTime entrada = DateTime.Today.AddHours(8);
            DateTime saida = DateTime.Today.AddHours(14);
            await acessoRepository.Adicionar(AcessoColaborador.Criar(0, colaborador, entrada).Value!);
            Assert.True(await acessoRepository.EstaNaAcademia(colaborador.Id));

            await acessoRepository.Adicionar(AcessoColaborador.Criar(0, colaborador, saida).Value!);

            Assert.False(await acessoRepository.EstaNaAcademia(colaborador.Id));
            Assert.Equal(TimeSpan.FromHours(6),
                await acessoRepository.ObterHorasTrabalhadasNoDia(colaborador.Id, DateOnly.FromDateTime(DateTime.Today)));
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

    private static Colaborador CriarColaborador(Logradouro logradouro)
    {
        return Colaborador.Criar(
            0,
            "Colaborador Teste",
            "529.982.247-25",
            DateOnly.FromDateTime(DateTime.Today.AddYears(-30)),
            "(11) 91234-5678",
            "colaborador@teste.com",
            logradouro,
            "123",
            string.Empty,
            "Abcdef",
            Arquivo.Criar([1, 2, 3]).Value!,
            DateOnly.FromDateTime(DateTime.Today.AddYears(-1)),
            ColaboradorTipo.Atendente,
            ColaboradorVinculo.CLT).Value!;
    }
}
