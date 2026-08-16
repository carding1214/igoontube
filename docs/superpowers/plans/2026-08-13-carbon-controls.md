# Carbon Controls Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Pulir la interfaz WPF de PUPlayer con controles carbón reutilizables sin alterar la reproducción.

**Architecture:** Un `ResourceDictionary` concentra colores y plantillas. Las vistas consumen claves compartidas; el code-behind y los view models conservan sus contratos actuales.

**Tech Stack:** .NET 8, WPF/XAML, xUnit.

## Global Constraints

- Solo cambia la presentación XAML.
- Fondo negro, superficies carbón y ámbar suave para foco o selección.
- Objetivos interactivos de al menos 34 px y foco de teclado visible.
- Sin animaciones, historial, miniaturas, red ni cambios de reproducción.
- Ejecución directa, sin subagentes.

---

### Task 1: Cerrar el incremento de audio existente

**Files:**
- Modify: `src/PUPlayer.App/Playback/IPlayerBackend.cs`
- Modify: `src/PUPlayer.App/Playback/MpvWorkerBackend.cs`
- Modify: `src/PUPlayer.App/ViewModels/PlayerPanelViewModel.cs`
- Modify: `src/PUPlayer.App/Views/PlayerPanelView.xaml`
- Modify: `src/PUPlayer.App/Views/PlayerPanelView.xaml.cs`
- Test: `tests/PUPlayer.IntegrationTests/Playback/PlayerPanelViewModelTests.cs`

**Interfaces:**
- Consumes: `AudioSettings.ToMpvLavfi()` y solicitudes `set_audio_filter`/`load_external_audio`.
- Produces: presets y ajustes manuales independientes por panel.

- [ ] **Step 1: Verificar la prueba específica**

Run: `dotnet test tests/PUPlayer.IntegrationTests/PUPlayer.IntegrationTests.csproj -c Debug --filter AudioPreset_ChangesOnlyItsBackend`

Expected: PASS.

- [ ] **Step 2: Verificar la solución**

Run: `dotnet test PUPlayer.sln -c Debug`

Expected: 25 pruebas PASS, sin advertencias.

- [ ] **Step 3: Commit**

```powershell
git add src/PUPlayer.App tests/PUPlayer.IntegrationTests
git commit -m "feat: add per-panel audio controls"
```

### Task 2: Crear el tema carbón compartido

**Files:**
- Create: `src/PUPlayer.App/Themes/CarbonControls.xaml`
- Modify: `src/PUPlayer.App/App.xaml`
- Create: `tests/PUPlayer.IntegrationTests/UI/CarbonThemeTests.cs`

**Interfaces:**
- Consumes: recursos WPF de `Application.Resources`.
- Produces: `CarbonButton`, `CarbonIconButton`, `CarbonPrimaryButton`, `CarbonComboBox`, `CarbonSlider`, `CarbonCheckBox`, `CarbonExpander`, `MutedText` y pinceles semánticos.

- [ ] **Step 1: Escribir la prueba fallida**

```csharp
[Fact]
public void CarbonTheme_ExposesSharedControlStyles()
{
    var dictionary = LoadCarbonDictionary();
    foreach (var key in new[] { "CarbonButton", "CarbonIconButton", "CarbonPrimaryButton", "CarbonComboBox", "CarbonSlider", "CarbonCheckBox", "CarbonExpander" })
        Assert.True(dictionary.Contains(key), $"Missing {key}");
}
```

- [ ] **Step 2: Confirmar RED**

Run: `dotnet test tests/PUPlayer.IntegrationTests/PUPlayer.IntegrationTests.csproj -c Debug --filter CarbonTheme_ExposesSharedControlStyles`

Expected: FAIL porque `Themes/CarbonControls.xaml` no existe.

- [ ] **Step 3: Implementar el diccionario mínimo**

Crear colores semánticos y plantillas con `Border CornerRadius="8"`, altura mínima 34, hover grafito claro, pressed oscuro, foco ámbar y disabled atenuado. Fusionarlo desde `App.xaml`:

```xml
<ResourceDictionary Source="Themes/CarbonControls.xaml"/>
```

- [ ] **Step 4: Confirmar GREEN**

Run: `dotnet test tests/PUPlayer.IntegrationTests/PUPlayer.IntegrationTests.csproj -c Debug --filter CarbonTheme_ExposesSharedControlStyles`

Expected: PASS.

- [ ] **Step 5: Commit**

```powershell
git add src/PUPlayer.App/App.xaml src/PUPlayer.App/Themes tests/PUPlayer.IntegrationTests/UI
git commit -m "style: add shared carbon control theme"
```

### Task 3: Aplicar el tema a la experiencia completa

**Files:**
- Modify: `src/PUPlayer.App/MainWindow.xaml`
- Modify: `src/PUPlayer.App/Views/WorkspaceView.xaml`
- Modify: `src/PUPlayer.App/Views/PlayerPanelView.xaml`
- Modify: `DESIGN.md`
- Modify: `.impeccable/design.json`

**Interfaces:**
- Consumes: estilos y pinceles de `CarbonControls.xaml`.
- Produces: barra global, encabezados y controles visualmente consistentes, sin cambiar handlers ni bindings.

- [ ] **Step 1: Aplicar recursos compartidos**

Sustituir colores y estilos locales por `{DynamicResource ...}`. Usar botones vectoriales con `ToolTip`, línea de tiempo dominante, preset alineado y ajustes manuales dentro de un contenedor plegable.

- [ ] **Step 2: Compilar**

Run: `dotnet build PUPlayer.sln -c Debug`

Expected: PASS, sin errores ni advertencias XAML.

- [ ] **Step 3: Ejecutar todas las pruebas**

Run: `dotnet test PUPlayer.sln -c Debug --no-build`

Expected: todas PASS.

- [ ] **Step 4: Revisar la ventana real**

Abrir uno y dos videos; comprobar distribuciones horizontal/vertical, audio expandido, nombres largos, hover, foco, disabled y escalado. Corregir defectos en una sola tanda y hacer una confirmación final.

- [ ] **Step 5: Actualizar documentación y commit**

Actualizar tokens/componentes en `DESIGN.md` y `.impeccable/design.json`, después:

```powershell
git add src/PUPlayer.App DESIGN.md .impeccable/design.json
git commit -m "style: polish carbon player controls"
```
