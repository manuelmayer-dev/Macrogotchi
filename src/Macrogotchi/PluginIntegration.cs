using System.Text.Json;
using MacroDeck.Sdk;
using MacroDeck.Sdk.Actions;
using MacroDeck.Sdk.Ui;
using MacroDeck.Sdk.Widgets;
using MacroDeck.Ui.Components;
using MacroDeck.Ui.Config;
using MacroDeck.Ui.Dsl;
using MacroDeck.Ui.Model.Surfaces;
using MacroDeck.Ui.Runtime;
using Serilog;

namespace Macrogotchi;

public sealed class PluginIntegration : IPluginIntegration, IWidgetTypeProvider, IUiProvider
{
	public const string WidgetTypeId = "pet";
	public const string DialogViewId = "pet";

	private readonly PetGame _game;
	private readonly ILogger _logger;

	public PluginIntegration(PetGame game, ILogger logger)
	{
		_game = game;
		_logger = logger.ForContext<PluginIntegration>();
		Actions = [new OpenPetAction()];
	}

	public IReadOnlyList<IActionDefinition> Actions { get; }

	public IReadOnlyList<UiSurfaceDeclaration> Surfaces { get; } =
	[
		new() { Kind = UiSurfaceKinds.Widget, SessionMode = UiSessionModes.Shared },
		new() { Kind = UiSurfaceKinds.Preview, SessionMode = UiSessionModes.Shared },
		new() { Kind = UiSurfaceKinds.Dialog, SessionMode = UiSessionModes.Exclusive },
		new() { Kind = UiSurfaceKinds.Config, SessionMode = UiSessionModes.Exclusive },
	];

	public Task InitializeAsync(IIntegrationContext context) => Task.CompletedTask;

	public Task ShutdownAsync() => Task.CompletedTask;

	public async Task InitializeAsync(IWidgetTypeProviderContext context, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(context);

		var registration = await context.RegisterWidgetTypeAsync(Descriptor, cancellationToken).ConfigureAwait(false);
		_logger.Information("Registered widget type {WidgetTypeId}.", registration.WidgetTypeId);
	}

	public IReadOnlyList<WidgetTypeDescriptor> GetWidgetTypes() => [Descriptor];

	public Task<IUiSession?> CreateSessionAsync(UiSessionRequest request, CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(request);

		var surface = request.Surface;

		return Task.FromResult<IUiSession?>(surface.Kind switch
		{
			UiSurfaceKinds.Widget or UiSurfaceKinds.Preview when ServesWidget(surface, UiWidgetSurfaceAttributes.WidgetType)
				=> Session(surface, sample: IsSample(surface), NameIn(surface, UiWidgetSurfaceAttributes.Data)),
			UiSurfaceKinds.Dialog when Attribute(surface, UiDialogSurfaceAttributes.ViewId) == DialogViewId
				=> Session(surface, sample: false, name: null),
			UiSurfaceKinds.Config when Attribute(surface, UiConfigSurfaceAttributes.EntryPoint) == UiConfigEntryPoints.WidgetConfig &&
				ServesWidget(surface, UiConfigSurfaceAttributes.WidgetType)
				=> ConfigSession(surface),
			_ => null,
		});
	}

	private static WidgetTypeDescriptor Descriptor => new(
		WidgetTypeId,
		Strings.Widgets.Pet.Name(),
		Strings.Widgets.Pet.Description(),
		DefaultData: """{"name":""}""",
		DataSchema: """{"type":"object","properties":{"name":{"type":"string","maxLength":24}}}""",
		HasConfiguration: true);

	private UiViewSession Session(UiSurface surface, bool sample, string? name)
	{
		var padding = PaddingFor(surface);

		if (sample)
		{
			return new UiViewSession(new UiView(surface, PetView.Build(new UiState<PetState>(PetState.Sample), null, padding, name)));
		}

		var watch = _game.Watch();

		try
		{
			return new UiViewSession(new UiView(surface, PetView.Build(watch.State, _game, padding, name)), watch);
		}
		catch
		{
			watch.Dispose();
			throw;
		}
	}

	private static UiViewSession ConfigSession(UiSurface surface)
	{
		var name = new UiState<string>(NameIn(surface, UiConfigSurfaceAttributes.WidgetData) ?? string.Empty);
		var root = new UiWidgetConfiguration
		{
			Key = "root",
			Properties = new UiWidgetProperties
			{
				Key = "properties",
				Children =
				[
					new UiStringInput
					{
						Key = "name",
						Label = Strings.Config.Name.Label(),
						Placeholder = Strings.Config.Name.Placeholder(),
						Binding = Bind.To(name),
						MaxLength = 24,
					},
				],
			},
		};

		return new UiViewSession(new UiView(surface, root));
	}

	private static bool ServesWidget(UiSurface surface, string typeAttribute)
		=> Attribute(surface, typeAttribute)?.EndsWith("::" + WidgetTypeId, StringComparison.Ordinal) == true;

	private static string? NameIn(UiSurface surface, string dataAttribute)
		=> surface.Attributes.TryGetValue(dataAttribute, out var data) &&
			data.ValueKind == JsonValueKind.Object &&
			data.TryGetProperty("name", out var name) &&
			name.ValueKind == JsonValueKind.String &&
			!string.IsNullOrWhiteSpace(name.GetString())
				? name.GetString()
				: null;

	private static bool IsSample(UiSurface surface)
		=> surface.Attributes.TryGetValue(UiWidgetSurfaceAttributes.Sample, out var sample) && sample.ValueKind == JsonValueKind.True;

	private static string? Attribute(UiSurface surface, string key)
		=> surface.Attributes.TryGetValue(key, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

	// A rounded tile eats into its own corners, so content is inset by the part of the radius a corner
	// cuts off, and never by less than a little breathing room. Capped in cell units so a large widget
	// keeps the same clearance as a small one.
	private static UiSize PaddingFor(UiSurface surface)
	{
		var radius = surface.Attributes.TryGetValue(UiWidgetSurfaceAttributes.CornerRadius, out var value) &&
			value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var parsed)
				? parsed
				: 12;
		var inset = Math.Max(6, (1 - (1 / Math.Sqrt(2))) * radius);

		return UiSize.Capped(inset / UiLength.Cell, inset);
	}
}
