namespace Macrogotchi;

/// <summary>
/// The pet's artwork as 12x12 pixel grids. A tree carries no bytes and a plugin has no upload path for
/// images yet, so every sprite is drawn from coloured cells instead of a resource.
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
	private const string _poop = "#8b5a2b";

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
		".dbbbbbbbbd.",
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
		".dbbbbbbbbd.",
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
		".dbbbbbbbbd.",
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
		"dbbbbbbbbbbd",
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

	// Drawn over the pet's bottom right corner while the pet has not been cleaned.
	private static readonly string[] _poopOverlay =
	[
		".k.",
		"kkk",
		"kkk",
	];

	private const int _poopTop = Size - 3;
	private const int _poopLeft = Size - 3;

	public static string? ColorAt(PetState pet, int row, int column)
	{
		ArgumentNullException.ThrowIfNull(pet);

		if (pet.IsAlive && pet.Poop > 0 && row >= _poopTop && column >= _poopLeft &&
			_poopOverlay[row - _poopTop][column - _poopLeft] == 'k')
		{
			return _poop;
		}

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
}
