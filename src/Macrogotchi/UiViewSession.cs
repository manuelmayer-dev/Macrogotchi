using MacroDeck.Sdk.Ui;
using MacroDeck.Ui.Model.Events;
using MacroDeck.Ui.Model.Nodes;
using MacroDeck.Ui.Model.Patches;
using MacroDeck.Ui.Runtime;

namespace Macrogotchi;

// Disposal detaches the view's handlers and releases whatever feeds the view from outside the session. The
// view owns nothing disposable itself, but anything that outlives it and writes into it keeps it alive.
internal sealed class UiViewSession : IUiSession
{
	private readonly UiView _view;
	private readonly IDisposable? _owned;
	private readonly EventHandler _onChanged;
	private readonly EventHandler<UiHandlerFaultEventArgs> _onFaulted;

	public UiViewSession(UiView view, IDisposable? owned = null)
	{
		_view = view;
		_owned = owned;
		_onChanged = (_, _) => Changed?.Invoke(this, EventArgs.Empty);
		_onFaulted = (_, fault)
			=> Faulted?.Invoke(this, new UiSessionFaultedEventArgs(fault.Exception.Message, fault.Exception));

		_view.Changed += _onChanged;
		_view.HandlerFaulted += _onFaulted;
	}

	public event EventHandler? Changed;

	public event EventHandler<UiSessionFaultedEventArgs>? Faulted;

	public UiTree BuildTree() => _view.Tree;

	public IReadOnlyList<UiPatch> DrainPatches() => _view.DrainPatches();

	public void Dispatch(UiEvent uiEvent) => _view.Dispatch(uiEvent);

	public ValueTask DisposeAsync()
	{
		_view.Changed -= _onChanged;
		_view.HandlerFaulted -= _onFaulted;
		_owned?.Dispose();

		return ValueTask.CompletedTask;
	}
}
