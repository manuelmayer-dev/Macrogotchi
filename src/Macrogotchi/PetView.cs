using MacroDeck.Localization;
using MacroDeck.Ui.Components;
using MacroDeck.Ui.Dsl;
using MacroDeck.Ui.Runtime;

namespace Macrogotchi;

/// <summary>Builds the pet tree shared by the deck widget, the picker sample and the dialog.</summary>
public static class PetView
{
	private const double _gap = 0.02;
	private const double _barsHeight = 0.06;
	private const double _statusSize = 0.07;
	private const double _spriteExtent = 0.46;
	private const double _controlsHeight = 0.16;
	private const double _iconSize = 0.08;

	public static UiElement Build(UiState<PetState> pet, PetGame? game, UiSize padding, string? name = null)
	{
		ArgumentNullException.ThrowIfNull(pet);

		return new UiStack
		{
			Key = "pet",
			Direction = UiComponentDirections.Vertical,
			Justify = UiComponentJustify.SpaceBetween,
			Padding = padding,
			Gap = _gap,
			Children =
			[
				new UiWhen
				{
					Key = "nameGate",
					Condition = () => name is not null,
					Content = () => new UiTextRun
					{
						Key = "name",
						Text = name,
						Size = _statusSize,
						MinSize = 0.04,
						Align = UiComponentAlignments.Center,
						Weight = UiComponentTextWeights.Bold,
					},
				},
				BuildBars(pet),
				new UiTextRun
				{
					Key = "status",
					Text = UiText.FromLocalized(() => StatusOf(pet.Value)),
					Size = _statusSize,
					MinSize = 0.04,
					Align = UiComponentAlignments.Center,
					Role = UiComponentTextRoles.Secondary,
				},
				BuildSprite(pet),
				new UiWhen
				{
					Key = "controlsGate",
					Condition = () => pet.Value.IsAlive,
					Content = () => BuildControls(pet, game),
				},
				new UiWhen
				{
					Key = "resetGate",
					Condition = () => !pet.Value.IsAlive,
					Content = () => new UiStack
					{
						Key = "resetRow",
						Direction = UiComponentDirections.Horizontal,
						MainSize = _controlsHeight,
						Children =
						[
							Button("reset", "🥚", game is null ? null : game.Reset, Strings.Controls.NewEgg()),
						],
					},
				},
			],
		};
	}

	private static UiStack BuildBars(UiState<PetState> pet) => new()
	{
		Key = "bars",
		Direction = UiComponentDirections.Horizontal,
		MainSize = _barsHeight,
		Gap = 0.03,
		Children =
		[
			Bar("food", "🍖", () => pet.Value.Fullness, "#f97316", "#fdba74"),
			Bar("happy", "😊", () => pet.Value.Happiness, "#ec4899", "#f9a8d4"),
			Bar("energy", "⚡", () => pet.Value.Energy, "#3b82f6", "#93c5fd"),
		],
	};

	private static UiStack Bar(string key, string icon, Func<int> value, string startColor, string endColor) => new()
	{
		Key = key,
		Fill = true,
		Direction = UiComponentDirections.Horizontal,
		Align = UiComponentAlignments.Center,
		Gap = 0.01,
		Children =
		[
			new UiTextRun { Key = key + "Icon", Text = icon, Size = 0.05, MainSize = 0.06 },
			new UiRangeBar
			{
				Key = key + "Bar",
				Fill = true,
				Start = 0,
				End = UiValue.From(() => value() / (double)PetState.Max),
				StartColor = startColor,
				EndColor = endColor,
				Thickness = 0.03,
			},
		],
	};

	private static UiLayer BuildSprite(UiState<PetState> pet)
	{
		var rows = new UiElement[PetSprites.Size];

		for (var row = 0; row < PetSprites.Size; row++)
		{
			var cells = new UiElement[PetSprites.Size];

			for (var column = 0; column < PetSprites.Size; column++)
			{
				var (r, c) = (row, column);
				cells[column] = new UiStack
				{
					Key = $"px{r}_{c}",
					Fill = true,
					Background = UiValue.Optional(() => PetSprites.ColorAt(pet.Value, r, c) is { } color
						? UiValue.Of(color)
						: UiValue.None<string>()),
				};
			}

			rows[row] = new UiStack
			{
				Key = $"row{row}",
				Fill = true,
				Direction = UiComponentDirections.Horizontal,
				Children = cells,
			};
		}

		return new UiLayer
		{
			Key = "petArea",
			MainSize = _spriteExtent,
			Children =
			[
				new UiStack
				{
					Key = "spriteRow",
					Direction = UiComponentDirections.Horizontal,
					Justify = UiComponentJustify.Center,
					Children =
					[
						new UiStack
						{
							Key = "sprite",
							MainSize = _spriteExtent,
							Direction = UiComponentDirections.Vertical,
							Children = rows,
						},
					],
				},
				new UiStack
				{
					Key = "badges",
					Direction = UiComponentDirections.Horizontal,
					Justify = UiComponentJustify.End,
					Children =
					[
						new UiTextRun
						{
							Key = "badgeText",
							MainSize = 0.2,
							Size = _iconSize,
							Align = UiComponentAlignments.End,
							Text = UiText.From(() => BadgesOf(pet.Value)),
						},
					],
				},
			],
		};
	}

	private static UiStack BuildControls(UiState<PetState> pet, PetGame? game) => new()
	{
		Key = "controls",
		Direction = UiComponentDirections.Horizontal,
		MainSize = _controlsHeight,
		Gap = _gap,
		Children =
		[
			Button("feed", "🍚", game is null ? null : game.Feed),
			Button("snack", "🍬", game is null ? null : game.Snack),
			Button("play", "🎮", game is null ? null : game.Play),
			Button("clean", "🧹", game is null ? null : game.Clean),
			Button("heal", "💊", game is null ? null : game.Heal),
			Button("sleep", UiText.From(() => pet.Value.IsAsleep ? "☀️" : "💡"),
				game is null ? null : game.ToggleSleep),
		],
	};

	private static UiButton Button(string key, UiText icon, Action? press, UiText label = default) => new()
	{
		Key = key,
		Fill = true,
		Justify = UiComponentJustify.Center,
		Gap = 0.01,
		Background = "#334155",
		Events = press is null ? [] : [UiEventHandler.On(UiComponentEvents.Press, press)],
		Children =
		[
			new UiTextRun { Key = key + "Icon", Text = icon, Size = _iconSize, Align = UiComponentAlignments.Center },
			new UiWhen
			{
				Key = key + "LabelGate",
				Condition = () => label.IsDeclared,
				Content = () => new UiTextRun
				{
					Key = key + "Label",
					Text = label,
					Size = 0.05,
					Align = UiComponentAlignments.Center,
					Weight = UiComponentTextWeights.SemiBold,
				},
			},
		],
	};

	private static string BadgesOf(PetState pet)
	{
		if (!pet.IsAlive)
		{
			return string.Empty;
		}

		return string.Concat(pet.IsSick ? "🤒" : string.Empty, pet.IsAsleep ? "💤" : string.Empty);
	}

	private static LocalizedString StatusOf(PetState pet) => pet switch
	{
		{ IsAlive: false } => Strings.Status.Dead(),
		{ IsEgg: true } => Strings.Status.Egg(),
		{ IsAsleep: true } => Strings.Status.Sleeping(),
		{ IsSick: true } => Strings.Status.Sick(),
		{ Fullness: < 30 } => Strings.Status.Hungry(),
		{ Energy: < 20 } => Strings.Status.Tired(),
		{ Happiness: < 30 } => Strings.Status.Sad(),
		_ => Strings.Status.Happy(),
	};
}
