using AcademiaDoZe.Application.DTOs;
using AcademiaDoZe.Application.Enums;

namespace AcademiaDoZe.Application.Interfaces;

public interface IAcessoService
{
    Task<AcessoPessoaDto?> ObterPorCpfAsync(
        PessoaAcessoTipo tipo,
        string cpf,
        CancellationToken cancellationToken = default);

    Task<AcessoPessoaDto> RegistrarProximoAcessoAsync(
        PessoaAcessoTipo tipo,
        string cpf,
        CancellationToken cancellationToken = default);
}
