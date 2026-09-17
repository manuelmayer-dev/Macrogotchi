using MacroDeck.Ui.Components;
using MacroDeck.Ui.Dsl;

namespace Macrogotchi;

/// <summary>
/// The plugin's own vector icons, drawn as <c>ui.shape</c> paths. Emoji render differently on every client
/// and a text run ellipsizes a glyph wider than its font size, so nothing on the pet uses them.
/// </summary>
public static class PetIcons
{
	public static readonly string Food = new ShapePath()
		.Circle(0.6, 0.4, 0.32)
		.Bar(0.46, 0.54, 0.2, 0.8, 0.16)
		.Circle(0.25, 0.89, 0.1)
		.Circle(0.11, 0.75, 0.1)
		.ToString();

	public static readonly string Heart = new ShapePath()
		.Raw("M 0.5 0.9 C 0.18 0.68 0.04 0.5 0.06 0.32 C 0.08 0.14 0.32 0.04 0.5 0.24 " +
			"C 0.68 0.04 0.92 0.14 0.94 0.32 C 0.96 0.5 0.82 0.68 0.5 0.9 Z")
		.ToString();

	public static readonly string Energy = new ShapePath()
		.Polygon((0.6, 0.04), (0.16, 0.56), (0.46, 0.56), (0.38, 0.96), (0.84, 0.42), (0.54, 0.42), (0.66, 0.04))
		.ToString();

	public static readonly string Bowl = new ShapePath()
		.Raw("M 0.2 0.46 A 0.3 0.24 0 0 1 0.8 0.46 Z")
		.Raw("M 0.08 0.52 H 0.92 A 0.42 0.34 0 0 1 0.08 0.52 Z")
		.Rect(0.36, 0.86, 0.28, 0.07)
		.Circle(0.38, 0.3, 0.035, hole: true)
		.Circle(0.56, 0.36, 0.035, hole: true)
		.ToString();

	public static readonly string Candy = new ShapePath()
		.Circle(0.5, 0.5, 0.22)
		.Polygon((0.36, 0.5), (0.04, 0.26), (0.1, 0.5), (0.04, 0.74))
		.Polygon((0.64, 0.5), (0.96, 0.26), (0.9, 0.5), (0.96, 0.74))
		.Bar(0.42, 0.34, 0.58, 0.66, 0.06, hole: true)
		.ToString();

	public static readonly string Gamepad = new ShapePath()
		.RoundedRect(0.04, 0.26, 0.92, 0.42, 0.2)
		.Circle(0.22, 0.74, 0.16)
		.Circle(0.78, 0.74, 0.16)
		.Polygon(true, (0.26, 0.34), (0.34, 0.34), (0.34, 0.42), (0.42, 0.42), (0.42, 0.5), (0.34, 0.5),
			(0.34, 0.58), (0.26, 0.58), (0.26, 0.5), (0.18, 0.5), (0.18, 0.42), (0.26, 0.42))
		.Circle(0.64, 0.4, 0.05, hole: true)
		.Circle(0.76, 0.5, 0.05, hole: true)
		.ToString();

	public static readonly string Sparkles = new ShapePath()
		.Raw(Star(0.4, 0.56, 0.36))
		.Raw(Star(0.78, 0.22, 0.18))
		.Raw(Star(0.8, 0.8, 0.12))
		.ToString();

	public static readonly string Cross = new ShapePath()
		.RoundedRect(0.34, 0.08, 0.32, 0.84, 0.08)
		.RoundedRect(0.08, 0.34, 0.84, 0.32, 0.08)
		.ToString();

	public static readonly string Moon = new ShapePath()
		.Raw("M 0.56 0.06 A 0.44 0.44 0 1 0 0.94 0.62 A 0.34 0.34 0 0 1 0.56 0.06 Z")
		.ToString();

	public static readonly string Sun = BuildSun();

	public static readonly string Poop = new ShapePath()
		.RoundedRect(0.08, 0.66, 0.84, 0.28, 0.14)
		.RoundedRect(0.2, 0.44, 0.6, 0.26, 0.13)
		.RoundedRect(0.32, 0.24, 0.36, 0.24, 0.12)
		.Raw("M 0.44 0.26 Q 0.5 0.08 0.64 0.04 Q 0.62 0.16 0.66 0.3 Z")
		.ToString();

	public static readonly string Egg = new ShapePath()
		.Raw("M 0.5 0.04 C 0.78 0.04 0.88 0.46 0.88 0.62 C 0.88 0.84 0.72 0.96 0.5 0.96 " +
			"C 0.28 0.96 0.12 0.84 0.12 0.62 C 0.12 0.46 0.22 0.04 0.5 0.04 Z")
		.ToString();

	public static readonly string Thermometer = new ShapePath()
		.RoundedRect(0.38, 0.04, 0.24, 0.64, 0.12)
		.Circle(0.5, 0.74, 0.2)
		.Rect(0.46, 0.2, 0.08, 0.4, hole: true)
		.ToString();

	public static readonly string Sleep = new ShapePath()
		.Polygon(Z(0.38, 0.4, 0.58, 0.14))
		.Polygon(Z(0.06, 0.06, 0.3, 0.08))
		.ToString();

	/// <summary>
	/// An icon in a square box centred in the space it is given, so the unit-box path is never stretched.
	/// </summary>
	public static UiElement Icon(string key, UiValue<string> path, UiValue<string> color, UiSize mainSize = default, bool fill = false) => new UiModifier
	{
		Key = key,
		MainSize = mainSize,
		Fill = fill,
		Frame = new UiFrame { AspectRatio = 1 },
		Child = new UiShape
		{
			Key = key + "Shape",
			Shape = UiComponentShapes.Path,
			Path = path,
			Color = color,
		},
	};

	private static string Star(double cx, double cy, double r)
		=> FormattableString.Invariant(
			$"M {cx} {cy - r} Q {cx} {cy} {cx + r} {cy} Q {cx} {cy} {cx} {cy + r} Q {cx} {cy} {cx - r} {cy} Q {cx} {cy} {cx} {cy - r} Z");

	private static string BuildSun()
	{
		var path = new ShapePath().Circle(0.5, 0.5, 0.2);

		for (var i = 0; i < 8; i++)
		{
			var angle = i * Math.PI / 4;
			var (dx, dy) = (Math.Cos(angle), Math.Sin(angle));
			path.Bar(0.5 + (dx * 0.3), 0.5 + (dy * 0.3), 0.5 + (dx * 0.46), 0.5 + (dy * 0.46), 0.09);
		}

		return path.ToString();
	}

	// A "Z" of stroke t whose top-left corner is (x, y) and whose box is size x size.
	private static (double, double)[] Z(double x, double y, double size, double t)
	{
		var (right, bottom) = (x + size, y + size);
		return
		[
			(x, y), (right, y), (right, y + t), (x + (t * 1.3), bottom - t), (right, bottom - t),
			(right, bottom), (x, bottom), (x, bottom - t), (right - (t * 1.3), y + t), (x, y + t),
		];
	}
}
