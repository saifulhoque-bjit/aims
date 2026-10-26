#region using

using Catalog.Application.Dtos.Categories;
using Catalog.Application.Repositories;
using Catalog.Domain.Entities;
using Marten;
using Microsoft.EntityFrameworkCore;

#endregion

namespace Catalog.Application.Features.Category.Commands;

public record CreateCategoryCommand(CreateCategoryDto Dto, Actor Actor) : ICommand<Guid>;

public class CreateCategoryCommandValidator : AbstractValidator<CreateCategoryCommand>
{
    #region Ctors

    public CreateCategoryCommandValidator()
    {
        RuleFor(x => x.Dto)
            .NotNull()
            .WithMessage(MessageCode.BadRequest)
            .DependentRules(() =>
            {
                RuleFor(x => x.Dto.Name)
                    .NotEmpty()
                    .WithMessage(MessageCode.CategoryNameIsRequired);
            });
    }

    #endregion
}

public class CreateCategoryCommandHandler(IDocumentSession session, ISlugRepository slugRepository) : ICommandHandler<CreateCategoryCommand, Guid>
{
    #region Implementations

    public async Task<Guid> Handle(CreateCategoryCommand command, CancellationToken cancellationToken)
    {
        var dto = command.Dto;

        await session.BeginTransactionAsync(cancellationToken);

        var slug = dto.Name!.Slugify();
        if (await slugRepository.CategorySlugExistsAsync(slug, cancellationToken: cancellationToken))
        {
            throw new ClientValidationException(MessageCode.CategorySlugAlreadyExists, slug);
        }

        // Validate ParentId if provided
        if (dto.ParentId.HasValue)
        {
            var parentExists = await session.Query<CategoryEntity>()
                .AnyAsync(x => x.Id == dto.ParentId.Value, token: cancellationToken);

            if (!parentExists)
            {
                throw new ClientValidationException(MessageCode.CategoryIsNotExists, dto.ParentId.Value);
            }
        }

        var entity = CategoryEntity.Create(
            id: Guid.NewGuid(),
            name: dto.Name!,
            desctiption: dto.Description ?? string.Empty,
            slug: slug,
            performedBy: command.Actor.ToString(),
            parentId: dto.ParentId);

        session.Store(entity);
        await session.SaveChangesAsync(cancellationToken);

        return entity.Id;
    }

    #endregion
}
