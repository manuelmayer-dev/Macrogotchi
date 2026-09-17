using MacroDeck.Localization;
using MacroDeck.Ui.Components;
using MacroDeck.Ui.Dsl;
using MacroDeck.Ui.Runtime;

namespace Macrogotchi;

/// <summary>Builds the pet tree shared by the deck widget, the picker sample and the dialog.</summary>
public static class PetView
{
	private const double _gap = 0.025;
	private const double _statsHeight = 0.09;
	private const double _statusSize = 0.068;
	private const double _controlsHeight = 0.15;
	private const double _badgeSize = 0.13;

	private const string _buttonFace = "#262a33";
	private const string _foodColor = "#fb923c";
	private const string _happyColor = "#f472b6";
	private const string _energyColor = "#60a5fa";

	public static UiElement Build(UiState<PetState> pet, PetGame? game, UiSize padding, string? name = null)
	{
		ArgumentNullException.ThrowIfNull(pet);

		return new UiStack
		{
			Key = "pet",
			Direction = UiComponentDirections.Vertical,
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
						MinSize = 0.045,
						Align = UiComponentAlignments.Center,
						Weight = UiComponentTextWeights.Bold,
					},
				},
				new UiModifier
				{
					Key = "statsFade",
					MainSize = _statsHeight,
					Opacity = UiValue.From(() => pet.Value.IsAlive ? 1 : 0.35),
					Child = BuildStats(pet),
				},
				new UiTextRun
				{
					Key = "status",
					Text = UiText.FromLocalized(() => StatusOf(pet.Value)),
					Size = _statusSize,
					MinSize = 0.045,
					Align = UiComponentAlignments.Center,
					Weight = UiComponentTextWeights.SemiBold,
					Role = UiComponentTextRoles.Secondary,
				},
				BuildStage(pet),
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
					Content = () => BuildReset(game),
				},
			],
		};
	}

	private static UiStack BuildStats(UiState<PetState> pet) => new()
	{
		Key = "bars",
		Direction = UiComponentDirections.Horizontal,
		Align = UiComponentAlignments.Center,
		Gap = 0.04,
		Children =
		[
			Stat("food", PetIcons.Food, () => pet.Value.Fullness, _foodColor, "#fdba74"),
			Stat("happy", PetIcons.Heart, () => pet.Value.Happiness, _happyColor, "#f9a8d4"),
			Stat("energy", PetIcons.Energy, () => pet.Value.Energy, _energyColor, "#93c5fd"),
		],
	};

	private static UiStack Stat(string key, string icon, Func<int> value, string startColor, string endColor) => new()
	{
		Key = key,
		Fill = true,
		Direction = UiComponentDirections.Horizontal,
		Align = UiComponentAlignments.Center,
		Gap = 0.015,
		Children =
		[
			PetIcons.Icon(key + "Icon", icon, startColor, mainSize: _statsHeight),
			new UiRangeBar
			{
				Key = key + "Bar",
				Fill = true,
				Start = 0,
				End = UiValue.From(() => Math.Max(value(), 4) / (double)PetState.Max),
				StartColor = startColor,
				EndColor = endColor,
				Thickness = 0.035,
			},
		],
	};

	private static UiLayer BuildStage(UiState<PetState> pet) => new()
	{
		Key = "petArea",
		Fill = true,
		Children =
		[
			new UiModifier
			{
				Key = "sprite",
				Frame = new UiFrame { AspectRatio = 1 },
				Child = new UiLayer
				{
					Key = "spriteLayers",
					Children = [.. PetSprites.Palette.Select((color, index) => SpriteLayer(pet, color, index))],
				},
			},
			new UiStack
			{
				Key = "poopRow",
				Direction = UiComponentDirections.Horizontal,
				Justify = UiComponentJustify.End,
				Align = UiComponentAlignments.End,
				Gap = 0.005,
				Children =
				[
					PoopGate(pet, 3),
					PoopGate(pet, 2),
					PoopGate(pet, 1),
				],
			},
			new UiStack
			{
				Key = "badges",
				Direction = UiComponentDirections.Horizontal,
				Justify = UiComponentJustify.End,
				Align = UiComponentAlignments.Start,
				Children =
				[
					new UiStack
					{
						Key = "badgeColumn",
						MainSize = _badgeSize,
						Gap = 0.02,
						Children =
						[
							new UiWhen
							{
								Key = "sickGate",
								Condition = () => pet.Value is { IsAlive: true, IsSick: true },
								Content = () => PetIcons.Icon("sick", PetIcons.Thermometer, "#f87171", mainSize: _badgeSize),
							},
							new UiWhen
							{
								Key = "sleepGate",
								Condition = () => pet.Value is { IsAlive: true, IsAsleep: true },
								Content = () => PetIcons.Icon("asleep", PetIcons.Sleep, "#a5b4fc", mainSize: _badgeSize),
							},
						],
					},
				],
			},
		],
	};

	private static UiWhen PoopGate(UiState<PetState> pet, int count) => new()
	{
		Key = $"poop{count}Gate",
		Condition = () => pet.Value.IsAlive && pet.Value.Poop >= count,
		Content = () => PetIcons.Icon($"poop{count}", PetIcons.Poop, "#a16207", mainSize: 0.11),
	};

	private static UiShape SpriteLayer(UiState<PetState> pet, string color, int index) => new()
	{
		Key = $"layer{index}",
		Shape = UiComponentShapes.Path,
		Color = color,
		Path = UiValue.Optional(() => PetSprites.PathOf(pet.Value, color) is { } path
			? UiValue.Of(path)
			: UiValue.None<string>()),
	};

	private static UiStack BuildControls(UiState<PetState> pet, PetGame? game) => new()
	{
		Key = "controls",
		Direction = UiComponentDirections.Horizontal,
		MainSize = _controlsHeight,
		Gap = 0.02,
		Children =
		[
			Button("feed", PetIcons.Bowl, "#fdba74", game is null ? null : game.Feed, pet, state => state.Feed()),
			Button("snack", PetIcons.Candy, "#f9a8d4", game is null ? null : game.Snack, pet, state => state.Snack()),
			Button("play", PetIcons.Gamepad, "#c4b5fd", game is null ? null : game.Play, pet, state => state.Play()),
			Button("clean", PetIcons.Sparkles, "#67e8f9", game is null ? null : game.Clean, pet, state => state.Clean()),
			Button("heal", PetIcons.Cross, "#86efac", game is null ? null : game.Heal, pet, state => state.Heal()),
			new UiModifier
			{
				Key = "sleepState",
				Disabled = UiValue.From(() => !pet.Value.CanBeHandled),
				Child = new UiButton
				{
					Key = "sleep",
					Fill = true,
					Justify = UiComponentJustify.Center,
					Align = UiComponentAlignments.Center,
					Padding = 0.016,
					Background = _buttonFace,
					Events = game is null ? [] : [UiEventHandler.On(UiComponentEvents.Press, game.ToggleSleep)],
					Children =
					[
						PetIcons.Icon(
							"sleepIcon",
							UiValue.From(() => pet.Value.IsAsleep ? PetIcons.Sun : PetIcons.Moon),
							UiValue.From(() => pet.Value.IsAsleep ? "#fde68a" : "#a5b4fc"),
							fill: true),
					],
				},
			},
		],
	};

	// A press that would change nothing is shown as unavailable rather than silently ignored. The rule is
	// the game's own, applied to the snapshot, so the button and the action can never disagree.
	private static UiModifier Button(
		string key, string icon, string color, Action? press, UiState<PetState> pet, Func<PetState, PetState> rule) => new()
	{
		Key = key + "State",
		Disabled = UiValue.From(() => pet.Value is var current && ReferenceEquals(rule(current), current)),
		Child = new UiButton
		{
			Key = key,
			Fill = true,
			Justify = UiComponentJustify.Center,
			Align = UiComponentAlignments.Center,
			Padding = 0.016,
			Background = _buttonFace,
			Events = press is null ? [] : [UiEventHandler.On(UiComponentEvents.Press, press)],
			Children = [PetIcons.Icon(key + "Icon", icon, color, fill: true)],
		},
	};

	private static UiButton BuildReset(PetGame? game) => new()
	{
		Key = "reset",
		MainSize = _controlsHeight,
		Direction = UiComponentDirections.Horizontal,
		Justify = UiComponentJustify.Center,
		Align = UiComponentAlignments.Center,
		Gap = 0.03,
		Padding = 0.035,
		Background = _buttonFace,
		Events = game is null ? [] : [UiEventHandler.On(UiComponentEvents.Press, game.Reset)],
		Children =
		[
			PetIcons.Icon("resetIcon", PetIcons.Egg, "#f1f5f9", mainSize: 0.1),
			new UiTextRun
			{
				Key = "resetLabel",
				Text = Strings.Controls.NewEgg(),
				Size = 0.065,
				MinSize = 0.045,
				MainSize = 0.4,
				Weight = UiComponentTextWeights.SemiBold,
			},
		],
	};

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
