using MacroDeck.Ui.Runtime;

namespace Macrogotchi;

/// <summary>One session's reactive copy of the pet, kept current until it is disposed.</summary>
public sealed class PetWatch : IDisposable
{
	private readonly PetGame _game;
	private readonly Action _onChanged;
	private int _disposed;

	internal PetWatch(PetGame game)
	{
		_game = game;
		State = new UiState<PetState>(game.Current);
		_onChanged = () => State.Set(_game.Current);
		game.Changed += _onChanged;

		// A change that landed between reading Current above and subscribing must not be missed.
		State.Set(game.Current);
	}

	public UiState<PetState> State { get; }

	public void Dispose()
	{
		if (Interlocked.Exchange(ref _disposed, 1) == 0)
		{
			_game.Changed -= _onChanged;
		}
	}
}
