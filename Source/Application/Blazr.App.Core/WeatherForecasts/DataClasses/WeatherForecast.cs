/// ============================================================
/// Author: Shaun Curtis, Cold Elm Coders
/// License: Use And Donate
/// If you use it, donate something to a charity somewhere
/// ============================================================
namespace Blazr.App.Core;

public readonly record struct WeatherForecastUid(Guid Value);

public sealed record WeatherForecast : IDiodeEntity, ICommandEntity
{
    public WeatherForecastUid WeatherForecastUid { get; init; } = new WeatherForecastUid(Guid.NewGuid());

    public DateOnly Date { get; init; } 

    public int TemperatureC { get; init; }

    public string? Summary { get; init; }

    // IDiodeEntity interface implementation 
    public Guid Uid => WeatherForecastUid.Value;
}
