/// ============================================================
/// Author: Shaun Curtis, Cold Elm Coders
/// License: Use And Donate
/// If you use it, donate something to a charity somewhere
/// ============================================================

namespace Blazr.OneWayStreet.Diode;

public class DiodeCompositeFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IDataBroker _dataBroker;

    public DiodeCompositeFactory(IServiceProvider serviceProvider, IDataBroker dataBroker)
    {
        _serviceProvider = serviceProvider;
        _dataBroker = dataBroker;
    }

    /// <summary>
    /// Gets the data to populate a InvoiceComposite from the data store
    /// </summary>
    /// <param name="uid"></param>
    /// <returns></returns>
    public async Task<DiodeResult<DiodeComposite<TRootItem, TCollectionItem>>> GetFromDataProviderAsync<TRootItem, TCollectionItem>(ItemQueryRequest request)
        where TRootItem : class, IDiodeEntity, IEntity, new()
        where TCollectionItem : class, IDiodeEntity, IEntity, new()
    {
        // Gets the DI registered store from the DI Provider
        var contextProvider = _serviceProvider.GetService<DiodeCompositeProvider<TRootItem, TCollectionItem>>();

        // deal with a null store Provider
        if (contextProvider is null)
            return DiodeResult<DiodeComposite<TRootItem, TCollectionItem>>.Failure($"Could not locate a registered context Provider for {typeof(TRootItem).Name}/{typeof(TCollectionItem).Name}");

        var uid = request.Uid.Value;
        var store = contextProvider.GetContext(uid);

        // deal with an existing context
        if (store is not null)
            return DiodeResult<DiodeComposite<TRootItem, TCollectionItem>>.Failure($"A context already exists for {typeof(TRootItem).Name} and ID : {uid}");

        var result = await _dataBroker.ExecuteQueryAsync<DiodeCompositeData<TRootItem, TCollectionItem>>(new ItemQueryRequest(new EntityUid(uid)));

        if (!result.Successful || result.Item is null)
            return DiodeResult<DiodeComposite<TRootItem, TCollectionItem>>.Failure($"No entity retrieved from the data provider with a Uid of {uid}");

        var context = _serviceProvider.GetService<DiodeComposite<TRootItem, TCollectionItem>>()!;

        var loadResult = await context.LoadAsync(result.Item);

        contextProvider.AddContext(context);

        if (loadResult.Successful)
            return DiodeResult<DiodeComposite<TRootItem, TCollectionItem>>.Success(context);

        return DiodeResult<DiodeComposite<TRootItem, TCollectionItem>>.Failure(loadResult.Message ?? "Can't load context.");
    }

    /// <summary>
    /// Persists the Composite to the configured data store
    /// </summary>
    /// <returns></returns>
    public async Task<CommandResult> PersistToProviderAsync<TRootItem, TCollectionItem>(Guid uid)
        where TRootItem : class, IDiodeEntity, IEntity, new()
        where TCollectionItem : class, IDiodeEntity, IEntity, new()
    {
        // Gets the DI registered store from the DI Provider
        var contextProvider = _serviceProvider.GetService<DiodeCompositeProvider<TRootItem, TCollectionItem>>();

        // deal with a null store Provider
        if (contextProvider is null)
            return CommandResult.Failure($"Could not locate a registered context Provider for {typeof(TRootItem).Name}/{typeof(TCollectionItem).Name}");

        if (!contextProvider.TryGetContext(uid, out DiodeComposite<TRootItem, TCollectionItem>? context) || context.Root is null)
            return CommandResult.Failure($"No context exists with a Uid of {uid}");

        var invoiceAggregateData = new DiodeCompositeData<TRootItem, TCollectionItem>(context.Uid, context.Root.AsDiodeEntityData, context.ItemsAsEntityData);

        var state = context.Root.State.GetCommandState();

        var request = new CommandRequest<DiodeCompositeData<TRootItem, TCollectionItem>>(invoiceAggregateData, state);
        var result = await _dataBroker.ExecuteCommandAsync(request);

        await context.MarkAsPersistedAsync();

        return result;
    }
}
