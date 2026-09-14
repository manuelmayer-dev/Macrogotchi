using MacroDeck.Localization;
using MacroDeck.Sdk;
using MacroDeck.Sdk.Actions;
using MacroDeck.Sdk.Ui;

namespace Macrogotchi;

public sealed class OpenPetAction : IActionDefinition, IActionExecutor
{
	public string Id => "open-pet";

	public LocalizedText Name => Strings.Actions.Open.Name();

	public LocalizedText Description => Strings.Actions.Open.Description();

	public IReadOnlyList<ActionParameter> Parameters { get; } = [];

	public MacroDeckPlatform Platforms => MacroDeckPlatform.All;

	public IActionExecutor CreateExecutor() => this;

	public async Task<ActionResult> ExecuteAsync(ActionExecutionContext context)
	{
		ArgumentNullException.ThrowIfNull(context);

		if (context.Ui is null || string.IsNullOrEmpty(context.OriginClientId))
		{
			return ActionResult.Failed(ActionErrorCodes.Unavailable, Strings.Actions.Open.NoClient());
		}

		var opened = await context.Ui.ShowModalAsync(
			context.OriginClientId,
			new ModalDefinition { ViewId = PluginIntegration.DialogViewId, Title = Strings.Dialog.Title() },
			context.CancellationToken).ConfigureAwait(false);

		return opened
			? ActionResult.Success()
			: ActionResult.Failed(ActionErrorCodes.Unavailable, Strings.Actions.Open.NoClient());
	}
}
