/// ============================================================
/// Author: Shaun Curtis, Cold Elm Coders
/// License: Use And Donate
/// If you use it, donate something to a charity somewhere
/// ============================================================
namespace Blazr.OneWayStreet.Diode;

public static class DiodeStateExtensions
{
    /// <summary>
    /// Gets the correct Command State for Diode action markings
    /// </summary>
    /// <param name="entityState"></param>
    /// <returns></returns>
    public static CommandState GetCommandState(this DiodeState state)
    {
        // The order is is IMPORTANT

        // It's been added and then the decision make that it's not needed
        // So we don't need to persist anything
        if (state.IsNew && state.IsMarkedForDeletion)
            return CommandState.None;

        // It's new so needs adding to the provider
        if (state.IsNew)
            return CommandState.Add;

        // It's an existing record marked for deletion
        if (state.IsMarkedForDeletion)
            return CommandState.Delete;

        // It's an existing record that's been mutated
        // and therefore needs updating in the provider
        if (state.IsMutated)
            return CommandState.Update;

        return CommandState.None;
    }
}
