# Kebechet.Types.Health

The health data types of [Kebechet.Maui.Health](https://github.com/Kebechet/Maui.Health), without MAUI: the
DTOs (`WorkoutDto`, `HeartRateDto`, `StepsDto`...), enums (`ActivityType`, `HealthDataType`...), result types and
`WorkoutSession`.

Use it for code that a MAUI app shares with something that is not a MAUI app, such as a watch app or a backend:
messages, storage, mapping. A MAUI app doesn't reference it directly, because `Kebechet.Maui.Health` already does.

```bash
dotnet add package Kebechet.Types.Health
```

The types keep their `Maui.Health.*` namespaces, so code moves between the two packages unchanged. Both packages
ship together with the same version.

## License

[MIT](https://github.com/Kebechet/Maui.Health/blob/main/LICENSE)
