using System.Text.Json;
using MacroDeck.Plugin.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Serilog;

namespace Macrogotchi;

/// <summary>
/// The one pet this plugin keeps: its current snapshot, the clock that ages it, and the file it survives
/// restarts in. Every widget and dialog session renders its own <see cref="Watch" /> of that snapshot, so a
/// press on one deck repaints every other one.
/// </summary>
/// <remarks>
/// Views never read a state owned by the game. A <see cref="UiState{T}" /> keeps every view that ever read it
/// alive and flushes a patch into each one on every write, with no way to detach - so a shared state would pin
/// every closed session and grow its undrained patch queue every frame. A watch is owned by one session and
/// unhooked when that session closes, which lets the view go with it.
/// </remarks>
public sealed class PetGame : BackgroundService
{
	private static readonly TimeSpan _frameInterval = TimeSpan.FromSeconds(2);
	private static readonly TimeSpan _tickInterval = TimeSpan.FromMinutes(1);
	private const int _maxCatchUpTicks = 24 * 60;

	private readonly Lock _gate = new();
	private readonly string? _file;
	private readonly TimeProvider _time;
	private readonly ILogger _logger;
	private PetState _current;
	private bool _dirty;

	public PetGame(IOptions<PluginHostOptions> options, ILogger logger)
		: this(options?.Value.DataDirectory, TimeProvider.System, logger)
	{
	}

	public PetGame(string? dataDirectory, TimeProvider time, ILogger logger)
	{
		ArgumentNullException.ThrowIfNull(time);
		ArgumentNullException.ThrowIfNull(logger);

		_file = dataDirectory is null ? null : Path.Combine(dataDirectory, "pet.json");
		_time = time;
		_logger = logger.ForContext<PetGame>();
		_current = Load();
	}

	/// <summary>Raised after <see cref="Current" /> changed, outside the game's lock.</summary>
	internal event Action? Changed;

	/// <summary>The latest snapshot of the pet.</summary>
	public PetState Current
	{
		get
		{
			lock (_gate)
			{
				return _current;
			}
		}
	}

	/// <summary>
	/// A reactive copy of the pet for one session to render. Dispose it when the session closes, or the game
	/// keeps it - and the view reading it - alive for as long as the plugin runs.
	/// </summary>
	public PetWatch Watch() => new(this);

	public void Feed() => Apply(pet => pet.Feed());

	public void Snack() => Apply(pet => pet.Snack());

	public void Play() => Apply(pet => pet.Play());

	public void Clean() => Apply(pet => pet.Clean());

	public void Heal() => Apply(pet => pet.Heal());

	public void ToggleSleep() => Apply(pet => pet.ToggleSleep());

	public void Tick() => Apply(pet => pet.Tick());

	public void Reset() => Apply(_ => PetState.NewEgg(_time.GetUtcNow()));

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		using var timer = new PeriodicTimer(_frameInterval, _time);
		var nextTick = _time.GetUtcNow() + _tickInterval;

		while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
		{
			var now = _time.GetUtcNow();

			if (now >= nextTick)
			{
				nextTick = now + _tickInterval;
				Tick();
			}
			else
			{
				Advance(pet => pet.NextFrame(), persist: false);
			}

			Save();
		}
	}

	private void Apply(Func<PetState, PetState> rule) => Advance(rule, persist: true);

	private void Advance(Func<PetState, PetState> rule, bool persist)
	{
		lock (_gate)
		{
			var next = rule(_current);

			if (ReferenceEquals(_current, next))
			{
				return;
			}

			_current = persist ? next with { UpdatedAt = _time.GetUtcNow() } : next;
			_dirty |= persist;
		}

		// Raised outside the gate: a watch writes its view's state, which takes that view's lock, and a press
		// arrives holding the same lock before it reaches the gate. Each watch reads Current rather than a
		// passed value, so two writers notifying out of order still leave every watch on the latest snapshot.
		Changed?.Invoke();
	}

	private PetState Load()
	{
		var now = _time.GetUtcNow();

		if (_file is null || !File.Exists(_file))
		{
			return PetState.NewEgg(now);
		}

		try
		{
			var pet = JsonSerializer.Deserialize<PetState>(File.ReadAllText(_file)) ?? PetState.NewEgg(now);
			var missed = (int)Math.Clamp((now - pet.UpdatedAt) / _tickInterval, 0, _maxCatchUpTicks);

			for (var i = 0; i < missed; i++)
			{
				pet = pet.Tick();
			}

			return pet with { UpdatedAt = now };
		}
		catch (Exception exception) when (exception is JsonException or IOException)
		{
			_logger.Warning(exception, "The saved pet could not be read, starting with a new egg.");
			return PetState.NewEgg(now);
		}
	}

	private void Save()
	{
		PetState pet;

		lock (_gate)
		{
			if (!_dirty || _file is null)
			{
				return;
			}

			_dirty = false;
			pet = _current;
		}

		try
		{
			Directory.CreateDirectory(Path.GetDirectoryName(_file)!);
			File.WriteAllText(_file, JsonSerializer.Serialize(pet));
		}
		catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
		{
			_logger.Warning(exception, "The pet could not be saved.");
		}
	}
}
