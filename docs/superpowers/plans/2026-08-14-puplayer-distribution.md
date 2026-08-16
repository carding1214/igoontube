# PUPlayer Windows Distribution Plan

**Goal:** Entregar una carpeta portable y un instalador local bajo `F:\PUPlayer\dist`, sin descargas durante el uso.

### 1. Publicación portable

- [x] Publicar `PUPlayer.App` autocontenido `win-x64`.
- [x] Copiar Python IA, MediaPipe, Demucs y modelos; excluir cachés y temporales.
- [x] Verificar que la carpeta portable abre un archivo sin depender de C:.

### 2. Asociaciones consentidas

- [x] Registrar `PUPlayer.Video` y `Applications\PUPlayer.App.exe` bajo HKCU.
- [x] Añadir “Abrir con PUPlayer” para formatos compatibles.
- [x] Ofrecer asociación predeterminada solo mediante una tarea marcada por el usuario.
- [x] Desinstalar únicamente claves y archivos propios; conservar `.pucache`.

### 3. Instalador y pruebas

- [x] Compilar el instalador en `F:\PUPlayer\dist` con Inno Setup.
- [x] Probar instalación aislada, apertura múltiple y desinstalación.
- [x] Confirmar funcionamiento offline y ausencia de procesos residuales.
