# Macrogotchi

A virtual pet for Macro Deck 3, shipped as an out-of-process plugin. The pet lives in the plugin
process; every deck widget and dialog renders the same shared state through Macro Deck UI.

- **Widget** `com.suchbyte.macrogotchi::pet`: stat bars (food, happiness, energy), a status line, a
  pixel-art sprite drawn from coloured cells, and six buttons: feed, snack, play, clean, heal, sleep.
  A dead pet offers one button, a new egg. The optional widget configuration is the pet's name.
- **Action** `open-pet`: opens the same view as a modal on the device that pressed the button.
- **Rules**: one game tick per minute. The egg hatches after two ticks and grows through baby, child,
  teen and adult by age. Food and energy drain, meals turn into poop a few ticks later, two poops or
  five ticks of starving make the pet sick, and sickness or starving drains health until the pet dies.
  Sleeping restores energy. Missed ticks are replayed on start from the saved state in
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
