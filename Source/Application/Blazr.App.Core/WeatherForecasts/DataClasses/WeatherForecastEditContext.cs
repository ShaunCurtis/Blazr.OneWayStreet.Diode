﻿/// ============================================================
/// Author: Shaun Curtis, Cold Elm Coders
/// License: Use And Donate
/// If you use it, donate something to a charity somewhere
/// ============================================================

namespace Blazr.App.Core;

public class WeatherForecastEditContext : IDiodeAction, IDiodeMutation<WeatherForecast>
{
    public string ActionName => "Edit a WeatherForecast";

    public WeatherForecastUid WeatherForecastUid { get; private set; }

    public string? Summary { get; set; }

    public int TemperatureC { get; set; }

    public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.Now);

    public Guid Uid => WeatherForecastUid.Value;

    public WeatherForecastEditContext(WeatherForecast record)
    {
        this.WeatherForecastUid = record.WeatherForecastUid;
        this.Summary = record.Summary;
        this.TemperatureC = record.TemperatureC;
        this.Date = record.Date;
    }

    public DiodeAsyncMutationDelegate<WeatherForecast> Mutation => (DiodeMutationRequest<WeatherForecast> request) =>
        {
            var mutation = request.Item with { Date = this.Date, Summary = this.Summary, TemperatureC = this.TemperatureC };
            return Task.FromResult(DiodeResult<WeatherForecast>.Success(mutation));
        };
}
