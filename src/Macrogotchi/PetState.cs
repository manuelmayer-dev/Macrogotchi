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

	public PetState Tick()
	{
		if (!IsAlive)
		{
			return this;
		}

		var next = this with { AgeTicks = AgeTicks + 1 };

		if (next.IsEgg)
		{
			return next;
		}

		next = next with
		{
			Fullness = Clamp(Fullness - (IsAsleep ? 1 : 2)),
			Happiness = Clamp(Happiness - (IsAsleep ? 0 : Poop > 0 ? 2 : 1)),
			Energy = Clamp(Energy + (IsAsleep ? 5 : -1)),
		};

		if (Digesting > 0)
		{
			next = next.DigestTicks + 1 >= 3
				? next with { Poop = Math.Min(Poop + 1, 3), Digesting = Digesting - 1, DigestTicks = 0 }
				: next with { DigestTicks = DigestTicks + 1 };
		}

		next = next with { StarvingTicks = next.Fullness == 0 ? StarvingTicks + 1 : 0 };

		if (!next.IsSick && (next.Poop >= 2 || next.StarvingTicks >= 5))
		{
			next = next with { IsSick = true };
		}

		var healthDelta = next.IsSick ? -4 : next.Fullness == 0 ? -2 : next.Happiness == 0 ? -1 : 2;
		next = next with { Health = Clamp(Health + healthDelta) };

		if (next.Health == 0)
		{
			return next with { IsAlive = false, IsAsleep = false };
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

	public PetState Feed() => !CanBeHandled || IsAsleep || Fullness >= Max
		? this
		: this with { Fullness = Clamp(Fullness + 30), Digesting = Digesting + 1 };

	public PetState Snack() => !CanBeHandled || IsAsleep
		? this
		: this with
		{
			Fullness = Clamp(Fullness + 10),
			Happiness = Clamp(Happiness + 15),
			Health = Clamp(Health - (Fullness >= Max ? 5 : 0)),
		};

	public PetState Play() => !CanBeHandled || IsAsleep || Energy < 10
		? this
		: this with { Happiness = Clamp(Happiness + 20), Energy = Clamp(Energy - 10) };

	public PetState Clean() => !CanBeHandled ? this : this with { Poop = 0 };

	public PetState Heal() => !CanBeHandled || !IsSick
		? this
		: this with { IsSick = false, Health = Clamp(Health + 20), StarvingTicks = 0 };

	public PetState ToggleSleep() => !CanBeHandled ? this : this with { IsAsleep = !IsAsleep };

	public PetState NextFrame() => this with { Frame = Frame ^ 1 };

	private static int Clamp(int value) => Math.Clamp(value, 0, Max);
}
