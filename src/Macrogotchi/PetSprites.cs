using System.Runtime.CompilerServices;

namespace Macrogotchi;

/// <summary>
/// The pet's artwork as 12x12 pixel grids. A tree carries no bytes and a plugin has no upload path for
/// images yet, so every sprite is drawn as one vector path per colour instead of a resource.
/// </summary>
public static class PetSprites
{
	public const int Size = 12;

	private const string _outline = "#1e293b";
	private const string _accent = "#fb7185";
	private const string _cheek = "#fda4af";
	private const string _eggShell = "#f1f5f9";
	private const string _eggSpot = "#a5b4fc";
	private const string _stone = "#9ca3af";
	private const string _sickEye = "#65a30d";

	// A layer's rectangles overlap their neighbours by this much, so two colours meeting on a cell edge
	// never let the background show through an anti-aliased seam.
	private const double _bleed = 0.004;

	private static readonly ConditionalWeakTable<PetState, Dictionary<string, string>> _paths = new();

	// Row 0 stays empty in every sprite so the idle bounce (rows shifted up by one) never clips anything.
	private static readonly string[] _egg =
	[
		"............",
		".....dd.....",
		"...ddwwdd...",
		"..dwwwwwwd..",
		"..dwswwwwd..",
		".dwwwwwswwd.",
		".dwwwwwwwwd.",
		".dwswwwwwwd.",
		".dwwwwswwwd.",
		"..dwwwwwwd..",
		"..ddwwwwdd..",
		"....dddd....",
	];

	private static readonly string[] _baby =
	[
		"............",
		"............",
		"............",
		"....dddd....",
		"..ddbbbbdd..",
		".dbhbbbbbbd.",
		".dbebbbbebd.",
		".dcbMMMMbcd.",
		".dbbMMMMbbd.",
		".dbbbbbbbbd.",
		"..ddbbbbdd..",
		"....dddd....",
	];

	private static readonly string[] _child =
	[
		"............",
		"..d......d..",
		".dbd....dbd.",
		".dbbddddbbd.",
		".dbhbbbbbbd.",
		".dbebbbbebd.",
		".dbbbbbbbbd.",
		".dcbMMMMbcd.",
		".dbbMMMMbbd.",
		".dbbbbbbbbd.",
		"..dbbbbbbd..",
		"..dd.dd.dd..",
	];

	private static readonly string[] _teen =
	[
		"............",
		".....p......",
		".....d......",
		"...dddddd...",
		"..dbbbbbbd..",
		".dbhbbbbbbd.",
		".dbebbbbebd.",
		".dcbMMMMbcd.",
		".dbbMMMMbbd.",
		".dbbbbbbbbd.",
		"..dbbbbbbd..",
		"...dd..dd...",
	];

	private static readonly string[] _adult =
	[
		"............",
		"..p..pp..p..",
		"..pppppppp..",
		".ddbbbbbbdd.",
		"dbhbbbbbbbbd",
		"dbebbbbbbebd",
		"dbbbbbbbbbbd",
		"dcbbMMMMbbcd",
		"dbbbMMMMbbbd",
		".dbbbbbbbbd.",
		"..ddbbbbdd..",
		"..dd.dd.dd..",
	];

	private static readonly string[] _grave =
	[
		"............",
		"....dddd....",
		"...dggggd...",
		"..dggggggd..",
		"..dgggdggd..",
		"..dggdddgd..",
		"..dgggdggd..",
		"..dggggggd..",
		"..dggggggd..",
		"..dggggggd..",
		".dddddddddd.",
		"............",
	];


	/// <summary>Every colour a sprite can paint, in the order the layers are stacked.</summary>
	public static IReadOnlyList<string> Palette { get; } =
	[
		_eggShell, _eggSpot, _stone,
		BodyColor(PetStage.Baby), BodyColor(PetStage.Child), BodyColor(PetStage.Teen), BodyColor(PetStage.Adult),
		HighlightColor(PetStage.Baby), HighlightColor(PetStage.Child), HighlightColor(PetStage.Teen), HighlightColor(PetStage.Adult),
		_cheek, _accent, _sickEye, _outline,
	];

	/// <summary>
	/// The unit-box path covering every cell of <paramref name="pet" /> painted in <paramref name="color" />,
	/// or <c>null</c> when the sprite does not use it. Rows are merged into runs, so a layer is a handful of
	/// rectangles rather than one per cell.
	/// </summary>
	public static string? PathOf(PetState pet, string color)
	{
		ArgumentNullException.ThrowIfNull(pet);

		// Every layer of every view asks for the same snapshot, so the grid is walked once per state.
		var paths = _paths.GetValue(pet, BuildPaths);
		return paths.TryGetValue(color, out var path) ? path : null;
	}

	public static string? ColorAt(PetState pet, int row, int column)
	{
		ArgumentNullException.ThrowIfNull(pet);

		var sprite = pet.Mood == PetMood.Dead ? _grave : StageSprite(pet.Stage);
		var animated = pet.IsAlive && !pet.IsAsleep && pet.Frame == 1;
		var sourceRow = animated ? row + 1 : row;

		if (sourceRow >= Size)
		{
			return null;
		}

		var cell = sprite[sourceRow][column];

		if (cell == 'M')
		{
			cell = MouthAt(pet.Mood, sourceRow, column, sprite);
		}

		return cell switch
		{
			'd' => _outline,
			'b' => BodyColor(pet.Stage),
			'h' => pet.Mood == PetMood.Sleeping ? BodyColor(pet.Stage) : HighlightColor(pet.Stage),
			'c' => pet.Mood == PetMood.Happy ? _cheek : BodyColor(pet.Stage),
			'e' => pet.Mood switch
			{
				PetMood.Sleeping => BodyColor(pet.Stage),
				PetMood.Sick => _sickEye,
				_ => _outline,
			},
			'p' => _accent,
			'w' => _eggShell,
			's' => _eggSpot,
			'g' => _stone,
			_ => null,
		};
	}

	private static Dictionary<string, string> BuildPaths(PetState pet)
	{
		var builders = new Dictionary<string, ShapePath>(StringComparer.Ordinal);
		var cell = 1d / Size;

		for (var row = 0; row < Size; row++)
		{
			var column = 0;

			while (column < Size)
			{
				var color = ColorAt(pet, row, column);
				var start = column;

				while (column < Size && ColorAt(pet, row, column) == color)
				{
					column++;
				}

				if (color is null)
				{
					continue;
				}

				if (!builders.TryGetValue(color, out var builder))
				{
					builder = builders[color] = new ShapePath();
				}

				builder.Rect(
					Math.Max(0, (start * cell) - _bleed),
					Math.Max(0, (row * cell) - _bleed),
					((column - start) * cell) + (2 * _bleed),
					cell + (2 * _bleed));
			}
		}

		return builders.ToDictionary(pair => pair.Key, pair => pair.Value.ToString(), StringComparer.Ordinal);
	}

	// The mouth zone is two rows of four cells; each mood paints it from a 2x4 mask.
	private static char MouthAt(PetMood mood, int row, int column, string[] sprite)
	{
		var start = column;

		while (start > 0 && sprite[row][start - 1] == 'M')
		{
			start--;
		}

		var top = sprite[row - 1][column] != 'M';
		var mask = mood switch
		{
			PetMood.Happy => top ? "d..d" : ".dd.",
			PetMood.Sad or PetMood.Sick => top ? ".dd." : "d..d",
			PetMood.Sleeping => top ? "...." : ".d..",
			_ => top ? "...." : ".dd.",
		};

		return mask[column - start] == 'd' ? 'd' : 'b';
	}

	private static string[] StageSprite(PetStage stage) => stage switch
	{
		PetStage.Egg => _egg,
		PetStage.Baby => _baby,
		PetStage.Child => _child,
		PetStage.Teen => _teen,
		_ => _adult,
	};

	private static string BodyColor(PetStage stage) => stage switch
	{
		PetStage.Baby => "#7dd3fc",
		PetStage.Child => "#86efac",
		PetStage.Teen => "#fcd34d",
		_ => "#f9a8d4",
	};

	private static string HighlightColor(PetStage stage) => stage switch
	{
		PetStage.Baby => "#e0f2fe",
		PetStage.Child => "#dcfce7",
		PetStage.Teen => "#fef3c7",
		_ => "#fce7f3",
	};
}
