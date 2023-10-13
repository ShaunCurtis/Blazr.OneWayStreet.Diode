/// ============================================================
/// Author: Shaun Curtis, Cold Elm Coders
/// License: Use And Donate
/// If you use it, donate something to a charity somewhere
/// ============================================================

using Blazr.App.Core;
using Blazr.App.Infrastructure;
using Blazr.Diode;
using Blazr.OneWayStreet.Core;
using Blazr.OneWayStreet.Diode;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Blazr.Test;

public class WeatherForecastTests
{
    private TestDataProvider _testDataProvider;

    public WeatherForecastTests()
        => _testDataProvider = TestDataProvider.Instance();

    private ServiceProvider GetServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddAppServerMappedInfrastructureServices();
        services.AddLogging(builder => builder.AddDebug());

        var provider = services.BuildServiceProvider();

        // get the DbContext factory and add the test data
        var factory = provider.GetService<IDbContextFactory<InMemoryTestDbContext>>();
        if (factory is not null)
            TestDataProvider.Instance().LoadDbContext<InMemoryTestDbContext>(factory);

        return provider!;
    }

    [Fact]
    public async void GetAForecast()
    {
        // Get a fully stocked DI container
        var provider = GetServiceProvider();

        // Get the Diode Context Factory
        var factory = provider.GetService<DiodeContextFactory>()!;

        //Get the data broker
        var broker = provider.GetService<IDataBroker>()!;

        // Get the test item from the Test Provider
        var testDboItem = _testDataProvider.WeatherForecasts.First();
        // Gets the Id to retrieve
        var testUid = testDboItem.Uid;

        // Get the Domain object - the Test data provider deals in dbo objects
        var testItem = DboWeatherForecastMap.Map(testDboItem);

        // Build a Diode request instance
        var request = new DiodeEntityRequest(testUid, testUid);

        // Execute the query against the factory
        var loadResult = await factory.GetEntityFromProviderAsync<WeatherForecast>(request);
        // check the query was successful
        Assert.True(loadResult.Successful);

        // get the returned record 
        var dbItem = loadResult.Item.ImmutableItem;
        // check it matches the test record
        Assert.Equal(testItem, dbItem);
    }

    [Fact]
    public async void UpdateAForecast()
    {
        // Get a fully stocked DI container
        var serviceProvider = GetServiceProvider();

        // Get the Diode Context Factory
        var factory = serviceProvider.GetService<DiodeContextFactory>()!;

        // Get the Diode Provider
        var provider = serviceProvider.GetService<DiodeContextProvider<WeatherForecast>>()!;

        //Get the data broker
        var broker = serviceProvider.GetService<IDataBroker>()!;

        // Get the test item from the Test Provider
        var testDboItem = _testDataProvider.WeatherForecasts.First();
        // Gets the Id to retrieve
        var testUid = testDboItem.Uid;

        // Get the Domain object - the Test data provider deals in dbo objects
        var testItem = DboWeatherForecastMap.Map(testDboItem);

        var expectedCount = _testDataProvider.WeatherForecasts.Count();

        // Build a Diode request instance
        var request = new DiodeEntityRequest(testUid, testUid);

        // Execute the query against the factory
        var loadResult = await factory.GetEntityFromProviderAsync<WeatherForecast>(request);
        // check the query was successful
        Assert.True(loadResult.Successful);

        // Get the item and load a edit context action
        var diodeContext = loadResult.Item;
        var dbItem = loadResult.Item.ImmutableItem;
        var editContext = new WeatherForecastEditContext(dbItem);

        // Apply an edit to the context
        editContext.TemperatureC = editContext.TemperatureC + 10;

        // Create a test comparison object with the edit applied
        var expectedItem = dbItem with { TemperatureC = dbItem.TemperatureC + 10 };

        //Apply the mutation to the Diode Context
        var mutationResult = await provider.DispatchAsync(editContext);
        Assert.True(mutationResult.Successful);

        // Persist the contexct to to data store
        var persistResult = await factory.PersistEntityToProviderAsync<WeatherForecast>(testUid);
        Assert.True(persistResult.Successful);

        // Read the actual data straight from the database
        {
            var dbFactory = serviceProvider.GetService<IDbContextFactory<InMemoryTestDbContext>>()!;
            var dbContext = dbFactory.CreateDbContext();

            var actualCount = dbContext.weatherForecasts.Count();
            var actualItem = dbContext.weatherForecasts.First(item => item.Uid == testUid);

            Assert.Equal(expectedCount, actualCount);
            Assert.Equal(expectedItem, actualItem);
        }
    }

    [Fact]
    public async void DeleteAForecast()
    {
        // Get a fully stocked DI container
        var serviceProvider = GetServiceProvider();

        // Get the Diode Context Factory
        var factory = serviceProvider.GetService<DiodeContextFactory>()!;

        // Get the Diode Provider
        var provider = serviceProvider.GetService<DiodeContextProvider<WeatherForecast>>()!;

        //Get the data broker
        var broker = serviceProvider.GetService<IDataBroker>()!;

        // Get the test item from the Test Provider
        var testDboItem = _testDataProvider.WeatherForecasts.First();
        // Gets the Id to retrieve
        var testUid = testDboItem.Uid;

        // Get the Domain object - the Test data provider deals in dbo objects
        var testItem = DboWeatherForecastMap.Map(testDboItem);

        var expectedCount = _testDataProvider.WeatherForecasts.Count() - 1;

        // Build a Diode request instance
        var request = new DiodeEntityRequest(testUid, testUid);

        // Execute the query against the factory
        var loadResult = await factory.GetEntityFromProviderAsync<WeatherForecast>(request);
        // check the query was successful
        Assert.True(loadResult.Successful);

        // Mark the context as deleted
        var deleteResult = provider.MarkContextForDeletion(testUid);
        Assert.True(deleteResult.Successful);

        // Persist the contexct to to data store
        var persistResult = await factory.PersistEntityToProviderAsync<WeatherForecast>(testUid);
        Assert.True(persistResult.Successful);

        // Read the actual data straight from the database
        {
            var dbFactory = serviceProvider.GetService<IDbContextFactory<InMemoryTestDbContext>>()!;
            var dbContext = dbFactory.CreateDbContext();

            var actualCount = dbContext.weatherForecasts.Count();
            var actualItem = dbContext.weatherForecasts.FirstOrDefault(item => item.Uid == testUid);

            Assert.Equal(expectedCount, actualCount);
            Assert.Null(actualItem);
        }
    }

    [Fact]
    public async void AddAForecast()
    {
        // Get a fully stocked DI container
        var serviceProvider = GetServiceProvider();

        // Get the Diode Context Factory
        var factory = serviceProvider.GetService<DiodeContextFactory>()!;

        // Get the Diode Provider
        var provider = serviceProvider.GetService<DiodeContextProvider<WeatherForecast>>()!;

        //Get the data broker
        var broker = serviceProvider.GetService<IDataBroker>()!;

        var expectedCount = _testDataProvider.WeatherForecasts.Count() + 1;

        // Execute the query against the factory
        var loadResult = factory.CreateNewEntity<WeatherForecast>();
        // check the query was successful
        Assert.True(loadResult.Successful);

        // Get the item and load a edit context action
        var diodeContext = loadResult.Item;
        var dbItem = loadResult.Item.ImmutableItem;
        var editContext = new WeatherForecastEditContext(dbItem);

        // Gets the Id to retrieve
        var testUid = dbItem.Uid;

        // Apply an edit to the context
        editContext.TemperatureC = 10;
        editContext.Date = DateOnly.FromDateTime(DateTime.Now);
        editContext.Summary = "Testing";

        // Create a test comparison object with the edit applied
        var expectedItem = dbItem with { TemperatureC = 10, Date = DateOnly.FromDateTime(DateTime.Now), Summary = "Testing" };

        //Apply the mutation to the Diode Context
        var mutationResult = await provider.DispatchAsync(editContext);
        Assert.True(mutationResult.Successful);

        // Persist the contexct to to data store
        var persistResult = await factory.PersistEntityToProviderAsync<WeatherForecast>(testUid);
        Assert.True(persistResult.Successful);

        // Read the actual data straight from the database
        {
            var dbFactory = serviceProvider.GetService<IDbContextFactory<InMemoryTestDbContext>>()!;
            var dbContext = dbFactory.CreateDbContext();

            var actualCount = dbContext.weatherForecasts.Count();
            var actualItem = dbContext.weatherForecasts.First(item => item.Uid == testUid);

            Assert.Equal(expectedCount, actualCount);
            Assert.Equal(expectedItem, actualItem);
        }
    }
}
