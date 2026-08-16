# IgoonTube Redesign Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Renombrar la aplicación como IgoonTube, integrar la marca elegida, rediseñar la interfaz carbón/azul/blanco y añadir pantalla completa real por panel.

**Architecture:** Los namespaces internos permanecen `PUPlayer`; el ensamblado visible pasa a `IgoonTube`. `WorkspaceView` administrará directamente hasta dos `PlayerPanelView` para poder expandir el mismo control sin reiniciar mpv. `MpvSurface` notificará doble clic y movimiento; la ventana controlará el modo de monitor completo y un temporizador ocultará la barra.

**Tech Stack:** .NET 8, WPF, Win32/libmpv, xUnit, ImageGen, PNG/ICO, Inno Setup.

## Global Constraints

- Nombre exacto: `IgoonTube`.
- Logo: lente circular, onda horizontal y Play central; `I`/`Tube` blancos y `goon` azul.
- Paleta: carbón `#17191D`, grafito `#25282E`, blanco `#F1F5F7`, azul `#69D2E7`.
- Doble clic y `Esc` alternan pantalla completa real en el monitor actual.
- Los controles inferiores se ocultan tras dos segundos sin movimiento.
- El estado de reproducción, zoom, seguimiento y audio no se reinicia.

---

### Task 1: Activos finales y nombre del ejecutable

**Files:**
- Create: `src/PUPlayer.App/Assets/IgoonTube.png`
- Create: `src/PUPlayer.App/Assets/IgoonTube.ico`
- Modify: `src/PUPlayer.App/PUPlayer.App.csproj`
- Modify: `src/PUPlayer.App/MainWindow.xaml`
- Modify: `src/PUPlayer.App/MainWindow.xaml.cs`
- Modify: `tests/PUPlayer.IntegrationTests/App/TestApp.cs`
- Modify: `tests/PUPlayer.IntegrationTests/UI/CarbonThemeTests.cs`

**Interfaces:**
- Produces: `IgoonTube.exe`, icono embebido y título `archivo — IgoonTube`.

- [ ] Generar el icono final tomando como referencia `exec-e1aa2ead-cd81-4023-9f9a-6fdfe9d5cfe6.png`, sin palabra y con fondo removible.
- [ ] Eliminar el fondo, validar transparencia y crear PNG/ICO con tamaños 16, 24, 32, 48, 64, 128 y 256.
- [ ] Añadir pruebas que exijan `AssemblyName=IgoonTube`, `ApplicationIcon=Assets\IgoonTube.ico` y recursos existentes.
- [ ] Ejecutar las pruebas UI y confirmar RED.
- [ ] Configurar ensamblado, icono, título y evento Ready visible como `IgoonTube` sin renombrar namespaces.
- [ ] Actualizar `TestApp.AppExe` a `IgoonTube.exe`; ejecutar pruebas y confirmar GREEN.
- [ ] Commit: `feat: brand app as IgoonTube`.

### Task 2: Estado y señales de pantalla completa

**Files:**
- Create: `src/PUPlayer.Core/Fullscreen/FullscreenState.cs`
- Create: `tests/PUPlayer.Core.Tests/Fullscreen/FullscreenStateTests.cs`
- Modify: `src/PUPlayer.App/Playback/MpvSurface.cs`
- Modify: `src/PUPlayer.App/Views/PlayerPanelView.xaml.cs`

**Interfaces:**
- Produces: `FullscreenState.Enter(DateTimeOffset)`, `Move(DateTimeOffset)`, `Tick(DateTimeOffset)`, `Exit()` y propiedades `IsActive`, `AreControlsVisible`.
- Produces: eventos `PlayerPanelView.FullscreenRequested` y `PlayerPanelView.MouseActivity`.

- [ ] Escribir pruebas para entrada, movimiento, ocultación a los dos segundos y salida.
- [ ] Ejecutar las pruebas Core y confirmar RED.
- [ ] Implementar el estado mínimo y confirmar GREEN.
- [ ] Cambiar el doble clic de `MpvSurface` para emitir `DoubleClicked` una sola vez y emitir `MouseMoved` desde `WM_MOUSEMOVE`.
- [ ] Conectar ambos eventos en `PlayerPanelView` sin alterar clic, arrastre o rueda.
- [ ] Commit: `feat: add fullscreen interaction state`.

### Task 3: Panel persistente y pantalla completa real

**Files:**
- Modify: `src/PUPlayer.App/Views/WorkspaceView.xaml`
- Modify: `src/PUPlayer.App/Views/WorkspaceView.xaml.cs`
- Modify: `src/PUPlayer.App/MainWindow.xaml.cs`
- Modify: `tests/PUPlayer.IntegrationTests/UI/CarbonThemeTests.cs`

**Interfaces:**
- Produces: `WorkspaceView.EnterFullscreen(PlayerPanelView)`, `ExitFullscreen()` y eventos hacia `MainWindow`.

- [ ] Añadir prueba STA que compruebe un host `Grid`, reutilización de la misma vista y ocultación del panel no elegido.
- [ ] Confirmar RED.
- [ ] Sustituir `ItemsControl/UniformGrid` por un host que mantenga una vista estable por ViewModel y actualice filas/columnas al añadir, reemplazar o cerrar.
- [ ] Expandir la vista seleccionada en el mismo host y conservar el otro panel oculto, no destruido.
- [ ] En `MainWindow`, guardar estilo/estado/bordes, entrar sin bordes en el monitor actual y restaurar con doble clic o `Esc`.
- [ ] Usar `DispatcherTimer` a 200 ms para ocultar controles/cursor después de dos segundos; cualquier movimiento los muestra.
- [ ] Ejecutar pruebas UI y una prueba manual con dos paneles.
- [ ] Commit: `feat: add per-video real fullscreen`.

### Task 4: Rediseño carbón, azul y blanco

**Files:**
- Modify: `src/PUPlayer.App/Themes/CarbonControls.xaml`
- Modify: `src/PUPlayer.App/Views/WorkspaceView.xaml`
- Modify: `src/PUPlayer.App/Views/PlayerPanelView.xaml`
- Modify: `tests/PUPlayer.IntegrationTests/UI/CarbonThemeTests.cs`

**Interfaces:**
- Produces: recursos `BrandBlueBrush`, `BrandWhiteBrush`, `BrandWordmark` y barra `ControlStrip` horizontal única.

- [ ] Añadir pruebas de paleta, cabecera de marca, controles en una línea, menú secundario y nombres accesibles.
- [ ] Confirmar RED.
- [ ] Reemplazar el naranja por azul, normalizar estados hover/pressed/focus y contraste.
- [ ] Crear cabecera compacta con icono y wordmark WPF: `I` blanco, `goon` azul, `Tube` blanco.
- [ ] Reorganizar tiempo, timeline, reproducción, volumen y acciones prioritarias en una sola fila; mantener ajustes secundarios en popup y desplazamiento horizontal solo como último recurso.
- [ ] Dar a la barra inferior visibilidad controlable en pantalla completa sin agregar overlays al modo ventana.
- [ ] Ejecutar pruebas UI, verificar 720 px, 1180 px y mosaico vertical/horizontal.
- [ ] Commit: `feat: redesign IgoonTube interface`.

### Task 5: Portable, asociaciones e instalador IgoonTube

**Files:**
- Modify: `scripts/publish-portable.ps1`
- Modify: `scripts/build-installer.ps1`
- Move: `installer/PUPlayer.iss` to `installer/IgoonTube.iss`
- Move: `docs/PUPlayer-LEEME.txt` to `docs/IgoonTube-LEEME.txt`
- Modify: `tests/PUPlayer.IntegrationTests/App/MultiProcessTests.cs`

**Interfaces:**
- Produces: `F:\PUPlayer\dist\IgoonTube\IgoonTube.exe`, `IgoonTube-Setup.exe` y `IgoonTube.Video`.

- [ ] Añadir comprobaciones de distribución para nombres, ejecutable, icono y comandos de asociación.
- [ ] Confirmar que fallan con los nombres antiguos.
- [ ] Cambiar staging/target, documentación y comandos sin renombrar `PUPlayer.MpvWorker.exe`.
- [ ] Actualizar Inno: AppName, publisher, carpeta `F:\IgoonTube`, accesos, icono, ProgID y formatos.
- [ ] Eliminar del directorio `dist` únicamente las entregas antiguas `PUPlayer*` después de validar las rutas exactas.
- [ ] Ejecutar pruebas y commit `build: distribute IgoonTube`.

### Task 6: Verificación y entrega

**Files:**
- Modify: `docs/manual-tests/vision-smoke.md`
- Modify: `docs/manual-tests/audio-smoke.md`
- Modify: `docs/superpowers/plans/2026-08-14-igoontube-redesign.md`

**Interfaces:**
- Verifies: ejecutable, portable, instalador y código fuente final.

- [ ] Ejecutar todos los tests Release; resultado esperado: cero fallos.
- [ ] Publicar portable y ejecutar self-test libmpv, imports CUDA/Demucs/MediaPipe y apertura del video proporcionado.
- [ ] Abrir dos procesos, alternar mosaicos, audio independiente, zoom/seguimiento y pantalla completa con barra automática.
- [ ] Compilar e instalar silenciosamente `IgoonTube-Setup.exe` en F:, probar apertura y desinstalar.
- [ ] Crear `F:\PUPlayer\dist\IgoonTube-Source.zip` desde HEAD.
- [ ] Limpiar solo temporales generados, comprobar repositorio limpio, hashes y ausencia de procesos residuales.
- [ ] Commit: `test: verify IgoonTube delivery`.
