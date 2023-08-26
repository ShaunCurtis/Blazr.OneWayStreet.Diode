/// ============================================================
/// Author: Shaun Curtis, Cold Elm Coders
/// License: Use And Donate
/// If you use it, donate something to a charity somewhere
/// ============================================================
namespace Blazr.OneWayStreet.Diode;

/// <summary>
/// The factory provides application specific mechanisms 
/// for creating and managing data retrieval to and from data stores
/// through OneWayStreet in Diode Context Providers
/// </summary>
public class DiodeContextFactory
{
    private IServiceProvider _serviceProvider;
    private IDataBroker _dataBroker;

    public DiodeContextFactory(IServiceProvider serviceProvider, IDataBroker dataBroker)
    {
        _serviceProvider = serviceProvider;
        _dataBroker = dataBroker;
    }

    /// <summary>
    /// Gets an Entity from OneWayStreet
    /// and loads it into its Diode Provider
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="request"></param>
    /// <returns></returns>
    public async Task<DiodeResult<DiodeContext<T>>> GetEntityFromProviderAsync<T>(ItemQueryRequest request)
    where T : class, IDiodeEntity, IEntity, new()
    {
        // Gets the DI registered store from the DI Provider
        var contextProvider = _serviceProvider.GetService<DiodeContextProvider<T>>();

        // deal with a null store Provider
        if (contextProvider is null)
            return DiodeResult<DiodeContext<T>>.Failure($"Could not locate a registered context Provider for {typeof(T).Name}");

        var uid = request.Uid.Value;
        var store = contextProvider.GetContext(uid);

        // deal with an existing context
        if (store is not null)
            return DiodeResult<DiodeContext<T>>.Failure($"A context already exists for {typeof(T).Name} and ID : {uid}");

        var result = await _dataBroker.ExecuteQueryAsync<T>(request);

        if (!result.Successful || result.Item is null)
            return DiodeResult<DiodeContext<T>>.Failure($"No entity retrieved from the data provider with a Uid of {request.Uid.Value}");

        var item = result.Item;

        var state = DiodeState.Existing();

        var contextResult = contextProvider.CreateContext(result.Item, state);

        return contextResult;
    }

    /// <summary>
    /// Creates a context for a new T in the Provider
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public DiodeResult<DiodeContext<T>> CreateNewEntity<T>(T? newEntity = null)
        where T : class, IDiodeEntity, IEntity, new()
    {
        // Gets the DI registered context from the DI Provider
        var contextProvider = _serviceProvider.GetService<DiodeContextProvider<T>>();

        // deal with a null store Provider
        if (contextProvider is null)
            return DiodeResult<DiodeContext<T>>.Failure($"Could not locate a registered context Provider for {typeof(T).Name}");

        T newItem = newEntity ?? new T();

        var state = DiodeState.New();

        var contextResult = contextProvider.CreateContext(newItem, state);

        return contextResult;
    }

    /// <summary>
    /// Persists the specific entity through OneWayStreet to the data store
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="uid"></param>
    /// <returns></returns>
    public async Task<CommandResult> PersistEntityToProviderAsync<T>(Guid uid)
        where T : class, IDiodeEntity, IEntity, new()
    {
        // Gets the DI registered context Provider
        var contextProvider = _serviceProvider.GetService<DiodeContextProvider<T>>();

        // deal with a null context Provider
        if (contextProvider is null)
            return CommandResult.Failure($"Could not locate a registered context Provider for {typeof(T).Name}");

        var context = contextProvider.GetContext(uid);

        // deal with a null store
        if (context is null)
            return CommandResult.Failure($"No context exists for {typeof(T).Name} and ID : {uid}");

        var state = context.State.GetCommandState();

        var request = new CommandRequest<T>(context.ImmutableItem, state);

        var result = await _dataBroker.ExecuteCommandAsync(request);

        contextProvider.MarkContextAsPersisted(uid);

        return result;
    }
}
