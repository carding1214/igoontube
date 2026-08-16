# PUPlayer Intimate Audio Design

## Objetivo

Conservar y realzar voz, gemidos y detalles cercanos como besos, lamidas, chupadas y sonidos húmedos sin clasificar escenas ni enviar audio fuera del equipo.

## Procesamiento

- `Íntimo detallado` será un preset DSP inmediato e independiente por panel.
- `Mejorar detalle con IA` reutilizará Demucs para separar voces.
- FFmpeg mezclará la pista vocal al frente con 22% del audio original. Así se conservan transitorios y detalles no vocales que una separación vocal pura puede eliminar.
- La mezcla aplicará presencia moderada, control de graves y compresión para evitar saturación.
- Se codificará como Opus `.mka`; nunca se duplicará el video.
- Usará una clave de caché distinta de `Mejorar voz`, de modo que ambos resultados puedan coexistir.

## Interfaz y errores

- El panel de ajustes ofrecerá el preset `Íntimo detallado` y el botón `Mejorar detalle con IA`.
- Progreso, cancelación, restauración del audio original y aislamiento por video seguirán el comportamiento existente.
- Si Demucs o FFmpeg fallan, el video conserva su audio actual y muestra un error breve.

## Validación

- Pruebas de valores DSP y filtro generado.
- Prueba de mezcla FFmpeg con dos señales conocidas.
- Prueba de caché independiente y reutilizable.
- Prueba manual con un fragmento temporal del video indicado por el usuario.
