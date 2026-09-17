# Macrogotchi

A virtual pet for Macro Deck 3, shipped as an out-of-process plugin. The pet lives in the plugin
process; every deck widget and dialog renders the same shared state through Macro Deck UI.

- **Widget** `com.suchbyte.macrogotchi::pet`: stat bars (food, happiness, energy), a status line, a
  pixel-art sprite drawn as one vector path per colour, and six buttons: feed, snack, play, clean, heal,
  sleep. Every icon is the plugin's own `ui.shape` path, not an emoji, and a button whose action would
  change nothing is shown disabled. A dead pet offers one button, a new egg. The optional widget
  configuration is the pet's name.
- **Action** `open-pet`: opens the same view as a modal on the device that pressed the button.
- **Rules**: one game tick per minute. The egg hatches after two ticks and grows through baby, child,
  teen and adult by age. Food, happiness and energy drain slowly, and a meal turns into poop twenty ticks
  later. Three poops or an hour of starving make the pet sick, and sickness or starving drains health until
  the pet dies - an untouched, well-fed pet lasts roughly ten hours. A dead pet leaves no mess. Sleeping
  restores energy. Up to twelve hours missed while the plugin was not running are replayed on start as
  rest: the pet ages and gets hungry, but never falls ill or loses health. The state is saved in
  `MACRO_DECK_PLUGIN_DATA_DIRECTORY/pet.json` (in-memory only when no data directory is set, as with
  `macrodeck-plugin run`).

## Develop

```bash
dotnet build
dotnet test
macrodeck-plugin run --project src/Macrogotchi
macrodeck-plugin test --project src/Macrogotchi
macrodeck-plugin build --source src/Macrogotchi --output ./artifacts
```

`run` pairs with the running Macro Deck host; approve the pairing prompt in Macro Deck (Developer Mode
must be on). Then add the Macrogotchi widget from the widget picker.
