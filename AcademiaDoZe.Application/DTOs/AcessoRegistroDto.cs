using AcademiaDoZe.Application.Enums;

namespace AcademiaDoZe.Application.DTOs;

public sealed class AcessoRegistroDto
{
    public required string Nome { get; init; }
    public required PessoaAcessoTipo TipoPessoa { get; init; }
    public required DateTime DataHora { get; init; }
    public required bool Entrada { get; init; }

    public string Evento => Entrada ? "Entrada" : "Saída";
    public string TipoPessoaTexto => TipoPessoa == PessoaAcessoTipo.Aluno ? "Aluno" : "Colaborador";
}
