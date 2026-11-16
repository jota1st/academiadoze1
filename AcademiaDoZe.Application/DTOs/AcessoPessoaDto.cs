using AcademiaDoZe.Application.Enums;

namespace AcademiaDoZe.Application.DTOs;

public sealed class AcessoPessoaDto
{
    public required int Id { get; init; }
    public required string Nome { get; init; }
    public required string Cpf { get; init; }
    public required PessoaAcessoTipo Tipo { get; init; }
    public required bool EstaNaAcademia { get; init; }
    public DateTime? UltimoAcesso { get; init; }

    public string Descricao => $"{Nome} — {Cpf}";
    public string Iniciais => string.IsNullOrWhiteSpace(Nome) ? "?" : Nome[..1].ToUpperInvariant();
    public string Situacao => EstaNaAcademia ? "Na academia" : "Fora da academia";
    public string ProximaAcao => EstaNaAcademia ? "Registrar saída" : "Registrar entrada";
}
