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
    public Task<DiodeResult<DiodeComposite<TRootItem, TCollectionItem>>> GetFromDataProviderAsync<TRootItem, TCollectionItem>(DiodeEntityRequest request)
        where TRootItem : class, IDiodeEntity, new()
        where TCollectionItem : class, IDiodeEntity, new()
    {
        // Get an instance of the type factory and use that to get the data 
        var factory = ActivatorUtilities.CreateInstance<DiodeCompositeFactory<TRootItem, TCollectionItem>>(_serviceProvider, _dataBroker);

        return factory.GetFromDataProviderAsync(request);
    }

    /// <summary>
    /// Persists the Composite to the configured data store
    /// </summary>
    /// <returns></returns>
    public Task<CommandResult> PersistToProviderAsync<TRootItem, TCollectionItem>(Guid uid)
        where TRootItem : class, IDiodeEntity, new()
        where TCollectionItem : class, IDiodeEntity, new()
    {
        // Get an instance of the type factory and use that to get the data 
        var factory = ActivatorUtilities.CreateInstance<DiodeCompositeFactory<TRootItem, TCollectionItem>>(_serviceProvider, _dataBroker);

        return factory.PersistToProviderAsync(uid);
    }

    /// <summary>
    /// Creates a new instance of a composite context
    /// Note that Composite Contexts must be registered in DI as Transient objects
    /// </summary>
    /// <typeparam name="TRootItem"></typeparam>
    /// <typeparam name="TCollectionItem"></typeparam>
    /// <returns></returns>
    public DiodeResult<DiodeComposite<TRootItem, TCollectionItem>> CreateCompositeContext<TRootItem, TCollectionItem>()
        where TRootItem : class, IDiodeEntity, new()
        where TCollectionItem : class, IDiodeEntity, new()
    {
        // Get an instance of the type factory and use that to get the data 
        var factory = ActivatorUtilities.CreateInstance<DiodeCompositeFactory<TRootItem, TCollectionItem>>(_serviceProvider, _dataBroker);

        return factory.CreateCompositeContext();
    }
}

public class DiodeCompositeFactory<TRootItem, TCollectionItem>
    where TRootItem : class, IDiodeEntity, new()
    where TCollectionItem : class, IDiodeEntity, new()
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
    public async Task<DiodeResult<DiodeComposite<TRootItem, TCollectionItem>>> GetFromDataProviderAsync(DiodeEntityRequest request)
    {
        // Gets the DI registered DI Provider
        var contextProvider = _serviceProvider.GetService<DiodeCompositeProvider<TRootItem, TCollectionItem>>();

        // deal with a null context Provider
        if (contextProvider is null)
            return DiodeResult<DiodeComposite<TRootItem, TCollectionItem>>.Failure($"Could not locate a registered context Provider for {typeof(TRootItem).Name}/{typeof(TCollectionItem).Name}");

        var uid = request.Uid;
        var store = contextProvider.GetContext(uid);

        // deal with an existing context
        if (store is not null)
            return DiodeResult<DiodeComposite<TRootItem, TCollectionItem>>.Failure($"A context already exists for {typeof(TRootItem).Name} and ID : {uid}");

        var result = await _dataBroker.ExecuteQueryAsync<DiodeCompositeData<TRootItem, TCollectionItem>>(ItemQueryRequest.Create(request.keyValue));

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
    public async Task<CommandResult> PersistToProviderAsync(Guid uid)
    {
        // Gets the DI registered provider from the DI Provider
        var contextProvider = _serviceProvider.GetService<DiodeCompositeProvider<TRootItem, TCollectionItem>>();

        // deal with a null store Provider
        if (contextProvider is null)
            return CommandResult.Failure($"Could not locate a registered context Provider for {typeof(TRootItem).Name}/{typeof(TCollectionItem).Name}");

        // get the composite context
        if (!contextProvider.TryGetContext(uid, out DiodeComposite<TRootItem, TCollectionItem>? context) || context.Root is null)
            return CommandResult.Failure($"No context exists with a Uid of {uid}");

        var composite = new DiodeCompositeData<TRootItem, TCollectionItem>(context.Uid, context.Root.AsDiodeEntityData, context.ItemsAsEntityData);

        var state = context.Root.State.GetCommandState();

        var request = new CommandRequest<DiodeCompositeData<TRootItem, TCollectionItem>>(composite, state);
        var result = await _dataBroker.ExecuteCommandAsync(request);

        await context.MarkAsPersistedAsync();

        return result;
    }

    /// <summary>
    /// Creates a new instance of a composite context
    /// Note that Composite Contexts must be registered in DI as Transient objects
    /// </summary>
    /// <typeparam name="TRootItem"></typeparam>
    /// <typeparam name="TCollectionItem"></typeparam>
    /// <returns></returns>
    public DiodeResult<DiodeComposite<TRootItem, TCollectionItem>> CreateCompositeContext()
    {
        // Gets the DI registered store from the DI Provider
        var context = _serviceProvider.GetService<DiodeComposite<TRootItem, TCollectionItem>>();

        // deal with a null store Provider
        if (context is null)
            return DiodeResult<DiodeComposite<TRootItem, TCollectionItem>>.Failure($"Could not locate a registered composite context for {typeof(TRootItem).Name}/{typeof(TCollectionItem).Name}");

        return DiodeResult<DiodeComposite<TRootItem, TCollectionItem>>.Success(context);
    }
}