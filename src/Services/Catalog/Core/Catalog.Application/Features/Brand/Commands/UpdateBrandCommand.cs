#region using

using Catalog.Application.Dtos.Brands;
using Catalog.Application.Repositories;
using Catalog.Domain.Entities;
using Marten;

#endregion

namespace Catalog.Application.Features.Brand.Commands;

public record UpdateBrandCommand(Guid BrandId, UpdateBrandDto Dto, Actor Actor) : ICommand<Guid>;

public class UpdateBrandCommandValidator : AbstractValidator<UpdateBrandCommand>
{
    #region Ctors

    public UpdateBrandCommandValidator()
    {
        RuleFor(x => x.BrandId)
            .NotEmpty()
            .WithMessage(MessageCode.BrandIdIsRequired);

        RuleFor(x => x.Dto)
            .NotNull()
            .WithMessage(MessageCode.BadRequest)
            .DependentRules(() =>
            {
                RuleFor(x => x.Dto.Name)
                    .NotEmpty()
                    .WithMessage(MessageCode.BrandNameIsRequired);
            });
    }

    #endregion
}

public class UpdateBrandCommandHandler(IDocumentSession session, ISlugRepository slugRepository) : ICommandHandler<UpdateBrandCommand, Guid>
{
    #region Implementations

    public async Task<Guid> Handle(UpdateBrandCommand command, CancellationToken cancellationToken)
    {
        await session.BeginTransactionAsync(cancellationToken);

        var entity = await session.LoadAsync<BrandEntity>(command.BrandId, cancellationToken)
            ?? throw new ClientValidationException(MessageCode.BrandIsNotExists, command.BrandId);

        var dto = command.Dto;

        // The brand's own slug is excluded, so a brand may keep its current name.
        var slug = dto.Name!.Slugify();
        if (await slugRepository.BrandSlugExistsAsync(slug, entity.Id, cancellationToken))
        {
            throw new ClientValidationException(MessageCode.BrandSlugAlreadyExists, slug);
        }

        entity.Update(
            name: dto.Name!,
            slug: slug,
            performedBy: command.Actor.ToString());

        session.Store(entity);
        await session.SaveChangesAsync(cancellationToken);

        return entity.Id;
    }

    #endregion
}
