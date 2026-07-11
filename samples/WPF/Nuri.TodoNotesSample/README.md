# Nuri Todo Notes Sample

Todo/notes sample for validating local state flow, controlled input, keyed list reorder, inline editing, and focus behavior.

Run it with:

```powershell
dotnet run --project "samples\Nuri.TodoNotesSample\Nuri.TodoNotesSample.csproj" -c Release
```

Behavior:

- Add notes from the composer at the top.
- Pin notes to move them to the top without changing their keys.
- Mark notes done, filter by state, and clear completed notes.
- Edit a note inline to pressure controlled text updates and focus timing.

This sample is intentionally a realistic productivity flow rather than a widget gallery. It should surface friction in local state shape, keyed row updates, inline edit ergonomics, and eventual focus/ref hooks.
