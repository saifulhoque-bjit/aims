#region using

using System.Reflection;
using AutoMapper;
using Catalog.Application.Dtos.Products;
using Catalog.Application.Features.Product.Commands;
using Catalog.Application.Services;
using Catalog.Domain.Entities;
using Common.Constants;
using Common.Models;
using Common.ValueObjects;
using Marten;
using MediatR;
using Xunit;

#endregion

namespace Catalog.Application.Tests.Features.Product.Commands;

public class CreateProductCommandHandlerTests
{
    #region Implementations

    [Fact]
    public async Task Handle_DoesNotThrow_WhenNameIsShorterThanSeoLimitAndSeoTitleIsEmpty()
    {
        var (handler, stored) = CreateHandler();

        var exception = await Record.ExceptionAsync(() =>
            handler.Handle(CreateCommand(name: "Dell XPS 15", seoTitle: null), CancellationToken.None));

        Assert.Null(exception);
        Assert.Equal("Dell XPS 15", Assert.Single(stored).SEOTitle);
    }

    [Fact]
    public async Task Handle_TruncatesNameToSixtyCharacters_WhenSeoTitleIsEmpty()
    {
        var (handler, stored) = CreateHandler();
        var name = new string('a', 75);

        await handler.Handle(CreateCommand(name: name, seoTitle: " "), CancellationToken.None);

        Assert.Equal(new string('a', 60), Assert.Single(stored).SEOTitle);
    }

    [Fact]
    public async Task Handle_KeepsExplicitSeoTitle()
    {
        var (handler, stored) = CreateHandler();

        await handler.Handle(CreateCommand(name: "Dell XPS 15", seoTitle: "Custom title"), CancellationToken.None);

        Assert.Equal("Custom title", Assert.Single(stored).SEOTitle);
    }

    #endregion

    #region Methods

    private static CreateProductCommand CreateCommand(string name, string? seoTitle) =>
        new(new CreateProductDto
        {
            Name = name,
            Sku = "SKU-1",
            ShortDescription = "short",
            LongDescription = "long",
            Price = 100m,
            SEOTitle = seoTitle,
            UploadThumbnail = new UploadFileBytes { FileName = "t.png", ContentType = "image/png", Bytes = [1] }
        }, Actor.User("tester"));

    private static (CreateProductCommandHandler Handler, List<ProductEntity> Stored) CreateHandler()
    {
        var stored = new List<ProductEntity>();

        // Stub: Marten session (external I/O). Store captures the entity; other calls are no-ops.
        var session = StubProxy<IDocumentSession>.Create((method, args) =>
        {
            if (method.Name == nameof(IDocumentSession.Store))
            {
                foreach (var item in (System.Collections.IEnumerable)args![0]!)
                {
                    stored.Add((ProductEntity)item);
                }
            }

            return null;
        });

        // Stub: MinIO upload (external I/O). Asserts the bucket and returns one uploaded file.
        var minIO = StubProxy<IMinIOCloudService>.Create((method, args) =>
        {
            if (method.Name == nameof(IMinIOCloudService.UploadFilesAsync))
            {
                Assert.Equal(AppConstants.Bucket.Products, args![1]);
                return Task.FromResult(new List<UploadFileResult> { new() { FileName = "t.png" } });
            }

            return null;
        });

        // Stub: mapper returns no image entity, so thumbnails are skipped.
        var mapper = StubProxy<IMapper>.Create((_, _) => null);

        // Stub: sender (only used for published products).
        var sender = StubProxy<ISender>.Create((_, _) => null);

        return (new CreateProductCommandHandler(mapper, session, minIO, sender), stored);
    }

    #endregion
}

public class StubProxy<T> : DispatchProxy where T : class
{
    #region Fields, Properties and Indexers

    private Func<MethodInfo, object?[]?, object?> _handler = (_, _) => null;

    #endregion

    #region Methods

    public static T Create(Func<MethodInfo, object?[]?, object?> handler)
    {
        var proxy = DispatchProxy.Create<T, StubProxy<T>>();
        ((StubProxy<T>)(object)proxy)._handler = handler;
        return proxy;
    }

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        var result = _handler(targetMethod!, args);
        if (result != null)
        {
            return result;
        }

        var returnType = targetMethod!.ReturnType;
        if (returnType == typeof(void))
        {
            return null;
        }

        if (returnType == typeof(Task))
        {
            return Task.CompletedTask;
        }

        if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
        {
            var valueType = returnType.GetGenericArguments()[0];
            var value = valueType.IsValueType ? Activator.CreateInstance(valueType) : null;
            return typeof(Task).GetMethod(nameof(Task.FromResult))!
                .MakeGenericMethod(valueType)
                .Invoke(null, [value]);
        }

        return returnType.IsValueType ? Activator.CreateInstance(returnType) : null;
    }

    #endregion
}
