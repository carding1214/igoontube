# PUPlayer Delivery Roadmap Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Entregar PUPlayer por fases utilizables hasta cubrir reproducción, mosaico, audio IA, seguimiento de sujeto y distribución para Windows.

**Architecture:** Una aplicación WPF sin instancia única aloja hasta dos superficies de video. Cada superficie usa un proceso mpv aislado; servicios locales compartidos atienden audio IA y visión mediante tuberías con nombre.

**Tech Stack:** .NET 8, WPF, libmpv, FFmpeg, Python embebido, MediaPipe, separación de fuentes local, xUnit, PowerShell.

## Global Constraints

- Windows 10/11 x64.
- Solo archivos locales; sin URLs ni streaming.
- Máximo dos paneles por ventana, sin control maestro.
- Sin historial, miniaturas, telemetría ni conexiones de red durante el uso.
- Proyecto, SDK, dependencias, modelos, temporales e instaladores bajo `F:\PUPlayer`.
- Caché persistente de IA solo de audio `.mka` Opus; nunca duplica video.

---

## Orden de entrega

- [x] **Fase 1 — Núcleo:** `2026-08-13-puplayer-core.md`; app reproducible con procesos independientes, mosaico, controles, audio simultáneo y zoom manual.
- [x] **Fase 2 — Audio:** presets DSP, volumen protegido a 200%, FFmpeg, separación local de voces, progreso/cancelación y caché Opus.
- [x] **Fase 3 — Visión:** captura reducida desde libmpv, selección de persona, seguimiento cuerpo/torso/cara y encuadre suavizado.
- [x] **Fase 4 — Distribución:** publicación autocontenida, asociación consentida de formatos, desinstalación, limpieza, pruebas de privacidad y rendimiento.

## Contratos entre fases

- `IPlayerBackend` es el único acceso de la interfaz a reproducción, audio y transformaciones.
- `WorkspaceViewModel` posee uno o dos `PlayerPanelViewModel`; ningún panel conoce al otro.
- `PUPlayer.MpvWorker` aloja libmpv y expone comandos/eventos por una tubería local autenticada con un token aleatorio por proceso.
- `PUPlayer.AIHost` será un proceso local compartido con colas separadas: visión prioritaria y una separación de audio simultánea.
- Los servicios devuelven datos en memoria o rutas bajo F; nunca reciben URLs.

## Cobertura de la especificación

| Requisito | Entrega |
|---|---|
| Ventanas independientes, mpv, barras, mosaico y zoom manual | Fase 1 |
| Volumen protegido, presets, FFmpeg, separación y caché Opus | Fase 2 |
| Selección y seguimiento de cuerpo, torso y cara | Fase 3 |
| Asociaciones, instalación en F, limpieza, privacidad y rendimiento | Fase 4 |
| Errores y pruebas de cada subsistema | Incluidos en su propia fase |
