using AcademiaDoZe.Application.DTOs;
using AcademiaDoZe.Application.Enums;

namespace AcademiaDoZe.Application.Interfaces;

public interface IAcessoService
{
    Task<IReadOnlyList<AcessoPessoaDto>> BuscarPessoasAsync(
        PessoaAcessoTipo tipo,
        string termo,
        CancellationToken cancellationToken = default);

    Task<AcessoPessoaDto?> ObterPorCpfAsync(
        PessoaAcessoTipo tipo,
        string cpf,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AcessoRegistroDto>> ObterAcessosDeHojeAsync(
        CancellationToken cancellationToken = default);

    Task<AcessoPessoaDto> RegistrarProximoAcessoAsync(
        PessoaAcessoTipo tipo,
        string cpf,
        CancellationToken cancellationToken = default);
}
