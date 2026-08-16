# IgoonTube — estado

- Repositorio: `F:\PUPlayer`
- Aplicación: `src\PUPlayer.App`
- Compilados: `F:\PUPlayer\dist`
- SDK y dependencias: locales en F:, mediante `scripts\dotnet.ps1`.

Implementado: ventanas independientes, dos paneles simultáneos, pantalla completa, audio 0–200%, presets manuales/IA, caché de audio, seguimiento, escenas, miniaturas, favoritos, clips, rotación, espejo, recorte, limpieza de caché y personalización global EN/ES.

Carga equilibrada implementada: ventana y estado de carga inmediatos, favoritos tras el primer fotograma, procesadores opcionales por panel y bajo demanda, caché global sin escaneo de inicio y read-ahead mpv acotado.

Distribuciones independientes: Full (`dist\IgoonTube`) y ligera (`dist\IgoonTube-NoAI`). NoAI conserva reproducción, audio manual, zoom, transformaciones, favoritos, miniaturas, caché y clips; excluye Python, modelos, separación IA, seguimiento y análisis automático.

Verificado el 2026-08-15: 141/141 pruebas aprobadas en Full y 141/141 en NoAI, incluyendo miniatura y clips original/transformado con el video real suministrado. Ambos portables y ambos instaladores se regeneraron sin firma.

Entrega gratuita sin firma digital. `dist\SHA256SUMS.txt` permite verificar ejecutables y archivos ZIP.

Verificación:

```powershell
$env:PUPLAYER_TEST_ROOT=(Get-Location).Path
$env:PUPLAYER_TEST_TEMP='F:\PUPlayer\work\tests'
powershell -ExecutionPolicy Bypass -File scripts\dotnet.ps1 test PUPlayer.sln -c Release --no-restore
powershell -ExecutionPolicy Bypass -File scripts\publish-portable.ps1
powershell -ExecutionPolicy Bypass -File scripts\publish-portable.ps1 -NoAI
powershell -ExecutionPolicy Bypass -File scripts\build-installer.ps1
powershell -ExecutionPolicy Bypass -File scripts\build-installer.ps1 -NoAI
```
