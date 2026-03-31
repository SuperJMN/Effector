# Effector Issue #10 Regression Sample

This sample was imported from the external repro attached to [issue #10](https://github.com/wieslawsoltes/Effector/issues/10) and is now stored in the Effector repo as a permanent regression harness.

It exercises the shader pipeline path used by `ISkiaShaderEffectFactory` on a visual with a non-identity `RenderTransform`. The original failure mode was anchor drift during shader overlay composition: when the shader was enabled and the host was scaled, the content shifted diagonally and clipped instead of remaining centered.

In this repo, the sample references the local `Effector` source and imports the repo's build targets directly, so it validates the current checkout rather than a published package.

## Expected behavior

1. Launch the sample.
2. Toggle the shader on.
3. Move the scale slider away from `1.0`, or run the pulse animation.
4. The blue `CONTENT` panel should stay centered and only scale.

If the content drifts diagonally or clips while the shader is active, the shader overlay composition path has regressed.

## Build

From the repository root:

```bash
dotnet build samples/Effector.Issue10.Repro/src/Desktop/Desktop.csproj -c Debug -m:1
dotnet run --project samples/Effector.Issue10.Repro/src/Desktop/Desktop.csproj -c Debug
```

You can also open the sample-specific solution:

```bash
samples/Effector.Issue10.Repro/Effector.Issue10.Repro.slnx
```

## Structure

```text
samples/Effector.Issue10.Repro/
  src/App/      Shared Avalonia app and shader effect
  src/Desktop/  Desktop head
```

## Notes

- The shader effect is deliberately simple. The goal is to isolate composition behavior, not shader logic.
- The sample remains useful for manual verification even when the bug is fixed, because it turns the original repro into a stable regression check.
