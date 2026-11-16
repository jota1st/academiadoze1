using AcademiaDoZe.Domain.Entities;
using AcademiaDoZe.Domain.Enums;
using AcademiaDoZe.Domain.ValueObjects;
namespace AcademiaDoZe.Domain.Tests.Entities;
// Marlon Rodrigues
public class AcessoColaboradorTests
{
    private static Logradouro GetValidLogradouro() => Logradouro.Criar(1, "12345-678", "Rua Teste", "Bairro", "Cidade", "SP", "Brasil").Value!;
    private static Arquivo GetValidArquivo() => Arquivo.Criar(new byte[] { 1, 2, 3 }).Value!;
    private static Colaborador GetValidColaborador() => Colaborador.Criar(1, "Fulano", "529.982.247-25", DateOnly.FromDateTime(DateTime.Today.AddYears(-30)), "(11) 91234-5678", "user@example.com", GetValidLogradouro(), "123", string.Empty, "Abcdef", GetValidArquivo(),
    DateOnly.FromDateTime(DateTime.Today.AddYears(-1)),
    ColaboradorTipo.Atendente,
    ColaboradorVinculo.CLT
    ).Value!;
    [Theory(DisplayName = "AcessoColaborador: colaborador nulo -> COLABORADOR_INVALIDO")]
    [InlineData(true)]
    [InlineData(false)]
    public void Deve_Falhar_Criacao_Quando_ColaboradorEhNulo(bool colaboradorNull)
    {
        var colaborador = colaboradorNull ? null : GetValidColaborador();
        var result = AcessoColaborador.Criar(1, colaborador!, DateTime.Today.AddHours(10));
        if (colaboradorNull)
        {
            Assert.True(result.IsFailure);
            Assert.NotEmpty(result.Notifications);
            Assert.Contains(result.Notifications, n => n.Mensagem == "COLABORADOR_INVALIDO");
        }
        else
        {
            Assert.True(result.IsSuccess);
        }
    }
    [Theory(DisplayName = "AcessoColaborador: aceita horário fora do intervalo dos alunos")]
    [InlineData(5, 59)]
    [InlineData(22, 1)]
    public void Deve_Permitir_Criacao_Quando_ColaboradorEstiverForaDoIntervaloDosAlunos(int hour, int minute)
    {
        var colaborador = GetValidColaborador();
        var dataHora = DateTime.Today.AddHours(hour).AddMinutes(minute);
        var result = AcessoColaborador.Criar(1, colaborador, dataHora);
        Assert.True(result.IsSuccess);
        Assert.Equal(colaborador.Id, result.Value!.Colaborador.Id);
        Assert.Equal(dataHora, result.Value.DataHora);
    }
    [Theory(DisplayName = "AcessoColaborador: criação bem-sucedida em horários permitidos")]
    [InlineData(10)]
    [InlineData(14)]
    public void Deve_Criar_Com_Sucesso_Quando_HorarioValido(int hour)
    {
        var colaborador = GetValidColaborador();
        var dataHora = DateTime.Today.AddHours(hour); // 10:00 or 14:00
        var result = AcessoColaborador.Criar(1, colaborador, dataHora);
        Assert.True(result.IsSuccess);
        Assert.Equal(colaborador.Id, result.Value!.Colaborador.Id);
        Assert.Equal(dataHora, result.Value.DataHora);
    }
    [Theory(DisplayName = "AcessoColaborador: aceita horários fora do turno do aluno")]
    [InlineData(5)]
    [InlineData(22)]
    [InlineData(23)]
    public void Deve_Permitir_HorariosForaDoTurnoDoAluno(int hour)
    {
        var colaborador = GetValidColaborador();
        var time = DateTime.Today.AddHours(hour);
        var r = AcessoColaborador.Criar(1, colaborador, time);
        Assert.True(r.IsSuccess);
    }
}