using System.Text.Json.Serialization;

namespace Macrogotchi;

public enum PetStage
{
	Egg,
	Baby,
	Child,
	Teen,
	Adult,
}

public enum PetMood
{
	Happy,
	Neutral,
	Sad,
	Sleeping,
	Sick,
	Dead,
}

/// <summary>
/// One immutable snapshot of the pet. Every rule is a pure function from one snapshot to the next, so the
/// game can replay the ticks that passed while the plugin was not running.
/// </summary>
public sealed record PetState
{
	public const int HatchTicks = 2;
	private const int _digestTicks = 20;
	public const int Max = 100;

	public int AgeTicks { get; init; }

	public int Fullness { get; init; } = Max;

	public int Happiness { get; init; } = Max;

	public int Energy { get; init; } = Max;

	public int Health { get; init; } = Max;

	public int Poop { get; init; }

	public int Digesting { get; init; }

	public int DigestTicks { get; init; }

	public int StarvingTicks { get; init; }

	public bool IsSick { get; init; }

	public bool IsAsleep { get; init; }

	public bool IsAlive { get; init; } = true;

	public int Frame { get; init; }

	public DateTimeOffset UpdatedAt { get; init; }

	[JsonIgnore]
	public PetStage Stage => AgeTicks switch
	{
		< HatchTicks => PetStage.Egg,
		< 30 => PetStage.Baby,
		< 120 => PetStage.Child,
		< 480 => PetStage.Teen,
		_ => PetStage.Adult,
	};

	[JsonIgnore]
	public bool IsEgg => Stage == PetStage.Egg;

	[JsonIgnore]
	public bool CanBeHandled => IsAlive && !IsEgg;

	[JsonIgnore]
	public PetMood Mood => this switch
	{
		{ IsAlive: false } => PetMood.Dead,
		{ IsAsleep: true } => PetMood.Sleeping,
		{ IsSick: true } => PetMood.Sick,
		{ Happiness: < 30 } or { Fullness: < 30 } => PetMood.Sad,
		{ Happiness: >= 70 } => PetMood.Happy,
		_ => PetMood.Neutral,
	};

	public static PetState NewEgg(DateTimeOffset now) => new() { UpdatedAt = now };

	public static PetState Sample { get; } = new()
	{
		AgeTicks = 600,
		Fullness = 80,
		Happiness = 90,
		Energy = 70,
	};

	/// <summary>One minute of the pet's life while the plugin runs.</summary>
	public PetState Tick() => Advance(offline: false);

	/// <summary>
	/// One minute that passed while the plugin was not running - overnight, say. The pet is treated as
	/// resting: it still ages, gets hungry slowly and digests, but it neither falls ill nor loses health, so
	/// switching the computer off is never what kills it.
	/// </summary>
	public PetState Rest() => Advance(offline: true);

	public PetState Feed() => !CanBeHandled || IsAsleep || Fullness >= Max
		? this
		: this with { Fullness = Clamp(Fullness + 25), Digesting = Math.Min(Digesting + 1, 3) };

	public PetState Snack() => !CanBeHandled || IsAsleep
		? this
		: this with
		{
			Fullness = Clamp(Fullness + 8),
			Happiness = Clamp(Happiness + 15),
			Health = Clamp(Health - (Fullness >= Max ? 5 : 0)),
		};

	public PetState Play() => !CanBeHandled || IsAsleep || Energy < 10
		? this
		: this with { Happiness = Clamp(Happiness + 15), Energy = Clamp(Energy - 8), Fullness = Clamp(Fullness - 2) };

	public PetState Clean() => !CanBeHandled || Poop == 0 ? this : this with { Poop = 0 };

	public PetState Heal() => !CanBeHandled || !IsSick
		? this
		: this with { IsSick = false, Health = Clamp(Health + 25), StarvingTicks = 0 };

	public PetState ToggleSleep() => !CanBeHandled ? this : this with { IsAsleep = !IsAsleep };

	public PetState NextFrame() => this with { Frame = Frame ^ 1 };

	// Rates are "one point every N minutes", keyed off the pet's age so no fractional state is stored.
	// Left completely alone, a well-fed pet goes hungry after about seven hours, falls ill an hour later
	// and dies roughly two hours after that - a working day, not a coffee break.
	private PetState Advance(bool offline)
	{
		if (!IsAlive)
		{
			return this;
		}

		var age = AgeTicks + 1;
		var next = this with { AgeTicks = age };

		if (next.IsEgg)
		{
			return next;
		}

		var resting = offline || IsAsleep;

		next = next with
		{
			Fullness = Clamp(Fullness - Every(age, resting ? 10 : 4)),
			Happiness = Clamp(Happiness - (resting ? 0 : Every(age, Poop > 0 ? 3 : 6))),
			Energy = Clamp(Energy + (IsAsleep || offline ? 1 : -Every(age, 6))),
		};

		if (Digesting > 0)
		{
			next = next.DigestTicks + 1 >= _digestTicks
				? next with { Poop = Math.Min(Poop + 1, 3), Digesting = Digesting - 1, DigestTicks = 0 }
				: next with { DigestTicks = DigestTicks + 1 };
		}

		next = next with { StarvingTicks = next.Fullness == 0 ? StarvingTicks + 1 : 0 };

		if (offline)
		{
			return next;
		}

		if (!next.IsSick && (next.Poop >= 3 || next.StarvingTicks >= 60))
		{
			next = next with { IsSick = true };
		}

		var healthDelta =
			-Every(age, 3) * ((next.IsSick ? 1 : 0) + (next.Fullness == 0 ? 1 : 0)) -
			(next.Happiness == 0 ? Every(age, 6) : 0);

		if (healthDelta == 0 && !next.IsSick && next.Fullness > 0)
		{
			healthDelta = Every(age, 2);
		}

		next = next with { Health = Clamp(Health + healthDelta) };

		if (next.Health == 0)
		{
			return Died(next);
		}

		if (next.IsAsleep && next.Energy >= Max)
		{
			return next with { IsAsleep = false };
		}

		if (!next.IsAsleep && next.Energy == 0)
		{
			return next with { IsAsleep = true };
		}

		return next;
	}

	// A grave has nothing left to digest or clean up.
	private static PetState Died(PetState pet) => pet with
	{
		IsAlive = false,
		IsAsleep = false,
		IsSick = false,
		Poop = 0,
		Digesting = 0,
		DigestTicks = 0,
	};

	private static int Every(int age, int minutes) => age % minutes == 0 ? 1 : 0;

	private static int Clamp(int value) => Math.Clamp(value, 0, Max);
}
