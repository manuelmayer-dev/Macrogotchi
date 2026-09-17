using System.Text.Json;
using MacroDeck.Plugin.Testing;
using MacroDeck.Ui.Runtime;
using MacroDeck.Ui.Testing;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Serilog;

namespace Macrogotchi.Tests;

[TestFixture]
public sealed class PetStateTests
{
	[Test]
	public void The_egg_hatches_after_the_hatch_ticks()
	{
		var pet = PetState.NewEgg(DateTimeOffset.UnixEpoch);

		Assert.That(pet.IsEgg, Is.True);
		Assert.That(pet.Feed(), Is.SameAs(pet));

		for (var i = 0; i < PetState.HatchTicks; i++)
		{
			pet = pet.Tick();
		}

		Assert.That(pet.Stage, Is.EqualTo(PetStage.Baby));
	}

	[Test]
	public void Feeding_fills_the_pet_and_produces_poop_later()
	{
		var pet = Hatched() with { Fullness = 40 };

		pet = pet.Feed();
		Assert.That(pet.Fullness, Is.EqualTo(65));

		pet = TickTimes(pet, 19);
		Assert.That(pet.Poop, Is.Zero);

		pet = pet.Tick();
		Assert.That(pet.Poop, Is.EqualTo(1));
		Assert.That(pet.Clean().Poop, Is.Zero);
	}

	[Test]
	public void An_ignored_pet_survives_a_working_day()
	{
		var pet = TickTimes(Hatched(), 8 * 60);

		Assert.That(pet.IsAlive, Is.True);
		Assert.That(pet.Fullness, Is.LessThan(PetState.Max));
	}

	[Test]
	public void A_starved_pet_gets_sick_and_dies_without_care()
	{
		var pet = Hatched() with { Fullness = 0 };

		pet = TickTimes(pet, 60);
		Assert.That(pet.IsSick, Is.True);
		Assert.That(pet.Heal().IsSick, Is.False);

		while (pet.IsAlive)
		{
			pet = pet.Tick();
		}

		Assert.That(pet.Mood, Is.EqualTo(PetMood.Dead));
		Assert.That(pet.Feed(), Is.SameAs(pet));
	}

	[Test]
	public void A_dead_pet_leaves_no_mess_behind()
	{
		var pet = Hatched() with { Fullness = 0, Health = 1, IsSick = true, Poop = 3, Digesting = 2 };

		pet = TickTimes(pet, 3);

		Assert.That(pet.IsAlive, Is.False);
		Assert.That(pet.Poop, Is.Zero);
		Assert.That(pet.Digesting, Is.Zero);
		Assert.That(pet.IsSick, Is.False);
	}

	[Test]
	public void Time_the_plugin_was_not_running_never_kills_the_pet()
	{
		var pet = Hatched() with { Fullness = 5, Health = 1, IsSick = true };

		for (var i = 0; i < 12 * 60; i++)
		{
			pet = pet.Rest();
		}

		Assert.That(pet.IsAlive, Is.True);
		Assert.That(pet.Health, Is.EqualTo(1));
	}

	[Test]
	public void Sleeping_restores_energy_and_wakes_the_pet_when_full()
	{
		var pet = Hatched() with { Energy = 98 };

		pet = pet.ToggleSleep();
		Assert.That(pet.Play(), Is.SameAs(pet));

		pet = TickTimes(pet, 2);

		Assert.That(pet.Energy, Is.EqualTo(PetState.Max));
		Assert.That(pet.IsAsleep, Is.False);
	}

	private static PetState TickTimes(PetState pet, int ticks)
	{
		for (var i = 0; i < ticks; i++)
		{
			pet = pet.Tick();
		}

		return pet;
	}

	private static PetState Hatched() => PetState.NewEgg(DateTimeOffset.UnixEpoch) with { AgeTicks = PetState.HatchTicks };
}

[TestFixture]
public sealed class PetViewTests
{
	[Test]
	public async Task Pressing_feed_updates_the_food_bar()
	{
		var game = GameWith(PetState.Sample with { Fullness = 40 });
		using var watch = game.Watch();
		var host = UiTestHost.Render(PetView.Build(watch.State, game, 0.05));

		var before = host.ById("pet.statsFade.bars.food.foodBar").Number("end") ?? 0;
		host.ById("pet.controls.feed").Raise("press");
		await host.SettleAsync();

		Assert.That(host.ById("pet.statsFade.bars.food.foodBar").Number("end"), Is.GreaterThan(before));
		Assert.That(game.Current.Fullness, Is.EqualTo(65));
	}

	[Test]
	public async Task A_closed_session_no_longer_follows_the_pet()
	{
		var game = GameWith(PetState.Sample with { Fullness = 40 });
		var watch = game.Watch();
		var host = UiTestHost.Render(PetView.Build(watch.State, game, 0.05));
		var before = host.ById("pet.statsFade.bars.food.foodBar").Number("end");

		watch.Dispose();
		game.Feed();
		await host.SettleAsync();

		Assert.That(game.Current.Fullness, Is.EqualTo(65));
		Assert.That(watch.State.Peek().Fullness, Is.EqualTo(40));
		Assert.That(host.ById("pet.statsFade.bars.food.foodBar").Number("end"), Is.EqualTo(before));
	}

	[Test]
	public void Every_open_watch_follows_the_pet()
	{
		var game = GameWith(PetState.Sample with { Fullness = 40 });
		using var first = game.Watch();
		using var second = game.Watch();

		game.Feed();

		Assert.That(first.State.Peek().Fullness, Is.EqualTo(65));
		Assert.That(second.State.Peek().Fullness, Is.EqualTo(65));
	}

	[Test]
	public void The_sample_draws_the_pet_but_offers_no_interaction()
	{
		var host = UiTestHost.Render(PetView.Build(new UiState<PetState>(PetState.Sample), null, 0.05));

		Assert.That(host.ById("pet.controls.feed").HasProperty("events"), Is.False);
		Assert.That(host.ById($"pet.petArea.sprite.spriteLayers.layer{PetSprites.Palette.Count - 1}").Text("path"), Is.Not.Null);
		Assert.That(host.ById("pet.statsFade.bars.food.foodIcon.foodIconShape").Text("path"), Is.EqualTo(PetIcons.Food));
	}

	private static PetGame GameWith(PetState pet)
	{
		var directory = Directory.CreateTempSubdirectory("macrogotchi-tests-").FullName;
		File.WriteAllText(
			Path.Combine(directory, "pet.json"),
			JsonSerializer.Serialize(pet with { UpdatedAt = DateTimeOffset.UtcNow }));

		return new PetGame(directory, TimeProvider.System, Log.Logger);
	}
}

[TestFixture]
public sealed class PluginIntegrationTests
{
	private static PluginTestHarness CreateHarness() =>
		PluginTestHarness.Create(builder => builder
			.UseLocalization(Strings.LocalizationCatalog)
			.ConfigureServices((_, services) => services.AddSingleton<PetGame>())
			.RegisterIntegration<PluginIntegration>());

	[Test]
	public async Task The_plugin_builds_and_initializes()
	{
		await using var harness = CreateHarness();

		Assert.DoesNotThrowAsync(harness.InitializeIntegrationsAsync);
	}

	[Test]
	public async Task The_open_action_fails_without_a_client_to_show_the_pet_on()
	{
		await using var harness = CreateHarness();
		await harness.InitializeIntegrationsAsync();

		var outcome = await harness.Actions.ExecuteAsync("open-pet", new Dictionary<string, object?>());

		Assert.That(outcome.Succeeded, Is.False);
	}
}

[TestFixture]
public sealed class LocalizationTests
{
	[Test]
	public void The_catalog_is_scoped_to_the_plugin_id()
	{
		Assert.That(Strings.LocalizationCatalog.Scope, Is.EqualTo("plugin:com.suchbyte.macrogotchi"));
	}

	[Test]
	public void Every_key_the_default_culture_declares_resolves_to_text_in_german_too()
	{
		foreach (var key in Strings.LocalizationCatalog.KeysOf("en"))
		{
			Assert.That(Strings.LocalizationCatalog.TryGetTemplate("de", key, out var text), Is.True, key);
			Assert.That(text, Is.Not.Empty);
		}
	}
}
