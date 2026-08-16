# Diseño de herramientas privadas de escena

## Objetivo

Añadir navegación discreta para contenido +18 mediante marcadores locales, bucle A–B, favoritos y una tecla de privacidad, sin copiar video ni enviar datos.

## Análisis híbrido

El análisis se inicia únicamente con el botón `Analizar escenas` de cada panel. Reutiliza Demucs para aislar voz y FFmpeg para medir intensidad, cambios espectrales y transitorios cercanos. Produce tres etiquetas neutrales: `Voz`, `Detalle` y `Actividad alta`. Un control de sensibilidad ajusta el umbral sin afirmar una clasificación semántica exacta.

El proceso es cancelable y no bloquea la reproducción. Solo se ejecuta uno a la vez para limitar GPU, CPU y disco.

## Caché y privacidad

Los resultados se guardan como JSON pequeño dentro de la carpeta `.pucache` del video. La clave incluye ruta normalizada, tamaño, fecha de modificación, versión del analizador y sensibilidad. No se guardan fragmentos, miniaturas ni nuevas copias de audio o video para esta función.

Si el archivo cambia, el índice se invalida. Un fallo elimina únicamente el resultado incompleto y deja intactos video, audio y cachés IA existentes.

## Línea de tiempo y favoritos

La línea de tiempo muestra marcas diferenciadas por categoría. Al pulsarlas se salta a la posición correspondiente. El usuario puede crear favoritos manuales con nombres discretos; se almacenan junto al índice sin modificar el archivo original.

## Bucle A–B

Cada panel tiene controles `A`, `B`, activar/desactivar repetición y limpiar. Al alcanzar B, el panel salta a A. Los puntos permanecen independientes y no afectan al otro video. No se persisten automáticamente entre sesiones.

## Modo privado

`Ctrl+Shift+M` aplica una acción global a la ventana: silencia todos los paneles, cambia los nombres visibles a `Video privado` y minimiza. Al restaurar, los videos permanecen silenciados hasta que el usuario ajuste el volumen manualmente. La posición y reproducción no se reinician.

## Errores y estados

- Sin video cargado, las acciones no hacen nada.
- B no puede quedar antes de A; la interfaz explica cómo corregirlo.
- Cancelar conserva cualquier índice válido anterior.
- Un JSON corrupto se ignora y puede regenerarse.
- Cerrar un panel cancela solamente su análisis pendiente.

## Verificación

- Pruebas de caché, invalidación, sensibilidad y JSON corrupto.
- Pruebas de salto, bucle y aislamiento entre dos paneles.
- Pruebas del atajo, nombres discretos, silencio y restauración.
- Prueba manual con el video proporcionado, sin conservar segmentos temporales.
