using System.Globalization;
using System.Text;

namespace Macrogotchi;

/// <summary>
/// Builds path data for a <c>ui.shape</c>: coordinates in the unit box, and only the absolute commands the
/// reader accepts (<c>M L H V C Q A Z</c>).
/// </summary>
/// <remarks>
/// A shape is filled with the non-zero rule, so the winding of every subpath matters: a filled part is
/// always wound clockwise and a hole counter-clockwise, which lets overlapping parts merge and a hole punch
/// through only where exactly one part lies under it.
/// </remarks>
internal sealed class ShapePath
{
	private readonly StringBuilder _data = new();

	public ShapePath Circle(double cx, double cy, double r, bool hole = false)
	{
		var sweep = hole ? 0 : 1;
		return Append($"M {N(cx - r)} {N(cy)} A {N(r)} {N(r)} 0 1 {sweep} {N(cx + r)} {N(cy)} A {N(r)} {N(r)} 0 1 {sweep} {N(cx - r)} {N(cy)} Z");
	}

	public ShapePath Polygon(bool hole, params (double X, double Y)[] points)
	{
		// Shoelace in a y-down space: a positive area is clockwise on screen.
		var area = 0d;

		for (var i = 0; i < points.Length; i++)
		{
			var (x1, y1) = points[i];
			var (x2, y2) = points[(i + 1) % points.Length];
			area += (x1 * y2) - (x2 * y1);
		}

		IEnumerable<(double X, double Y)> ordered = (area > 0) == !hole ? points : points.Reverse();
		var first = true;

		foreach (var (x, y) in ordered)
		{
			Append($"{(first ? "M" : "L")} {N(x)} {N(y)}");
			first = false;
		}

		return Append("Z");
	}

	public ShapePath Polygon(params (double X, double Y)[] points) => Polygon(false, points);

	public ShapePath Rect(double x, double y, double width, double height, bool hole = false)
		=> Polygon(hole, (x, y), (x + width, y), (x + width, y + height), (x, y + height));

	public ShapePath RoundedRect(double x, double y, double width, double height, double r)
	{
		var (right, bottom) = (x + width, y + height);
		return Append(
			$"M {N(x + r)} {N(y)} H {N(right - r)} A {N(r)} {N(r)} 0 0 1 {N(right)} {N(y + r)} " +
			$"V {N(bottom - r)} A {N(r)} {N(r)} 0 0 1 {N(right - r)} {N(bottom)} " +
			$"H {N(x + r)} A {N(r)} {N(r)} 0 0 1 {N(x)} {N(bottom - r)} " +
			$"V {N(y + r)} A {N(r)} {N(r)} 0 0 1 {N(x + r)} {N(y)} Z");
	}

	/// <summary>A bar of <paramref name="thickness" /> from one point to another, with square ends.</summary>
	public ShapePath Bar(double x1, double y1, double x2, double y2, double thickness, bool hole = false)
	{
		var length = Math.Sqrt(((x2 - x1) * (x2 - x1)) + ((y2 - y1) * (y2 - y1)));
		var (nx, ny) = (-(y2 - y1) / length * thickness / 2, (x2 - x1) / length * thickness / 2);

		return Polygon(hole, (x1 + nx, y1 + ny), (x2 + nx, y2 + ny), (x2 - nx, y2 - ny), (x1 - nx, y1 - ny));
	}

	/// <summary>Path data written by hand. Callers keep to the accepted commands and the winding rule.</summary>
	public ShapePath Raw(string data) => Append(data);

	public override string ToString() => _data.ToString();

	private ShapePath Append(string segment)
	{
		if (_data.Length > 0)
		{
			_data.Append(' ');
		}

		_data.Append(segment);
		return this;
	}

	private static string N(double value) => Math.Round(value, 4).ToString("0.####", CultureInfo.InvariantCulture);
}
