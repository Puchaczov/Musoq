namespace Musoq.Evaluator;

/// <summary>
/// Represents the method that will handle query phase change events.
/// </summary>
/// <param name="sender">The source of the event.</param>
/// <param name="args">The event arguments containing query ID and phase information.</param>
public delegate void QueryPhaseEventHandler(object sender, QueryPhaseEventArgs args);