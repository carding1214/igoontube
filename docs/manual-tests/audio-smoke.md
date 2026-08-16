# Prueba manual de audio

## Resultado verificado

- Release: 18 pruebas Core y 22 de integración superadas.
- libmpv: `libmpv: ok`.
- IA local: Python 3.11.15, Demucs 4.0.1, PyTorch 2.1.2 CUDA 12.1; `device=cuda`.
- Prueba real de 60 s: generó y cargó una caché vocal de 532,109 bytes; la segunda activación fue inmediata y no inició Demucs.
- Prueba de tamaño de 10 min: MKA Opus, una pista de audio, ninguna de video, 6,355,242 bytes frente a 52,920,078 bytes del WAV temporal.
- Al terminar no quedaron procesos de PUPlayer, Python ni FFmpeg bajo `F:\PUPlayer`.

## Comprobación manual

1. Abrir dos videos y confirmar reproducción, volumen, posición y ajustes independientes.
2. Dejar `Audio original` seleccionado; comprobar que no se aplica ningún filtro.
3. En Ajustes, probar presets y controles manuales por separado en ambos paneles.
4. Pulsar `Mejorar voz con IA`; confirmar progreso, cancelación y reproducción continua.
5. Cerrar y abrir el mismo video, activar IA y confirmar carga inmediata de la misma caché `.mka`.
6. Pulsar `Usar audio original` y confirmar que vuelve la pista interna.
7. Confirmar que la caché adyacente contiene solo `.mka` y `.json`; los WAV de trabajo deben desaparecer.

Los videos en C: usan `F:\PUPlayer\data\cache\audio`; el entorno, modelos y temporales permanecen en F:.

## Detalle íntimo verificado

- El preset aplica 35 Hz, +4 dB de voz, +6 dB de presencia y no usa reducción espectral.
- La IA mezcla voces con 22% del original, limita picos y genera únicamente audio Opus MKA.
- Fragmento real de 12 s: `Detalle íntimo activo` en 7 s y `Voz mejorada activa` en 4 s con RTX 3060/CUDA.
- Ambos modos usan claves de caché distintas y restauran el audio original.
