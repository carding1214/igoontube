# IgoonTube — personalización, idiomas y acabado final

## Objetivo

Completar los detalles pendientes de exportación y caché, añadir personalización global mediante presets y ajustes manuales, y ofrecer inglés predeterminado con español seleccionable. La distribución pública continuará sin firma digital.

## Configuración global

La configuración se guarda como JSON versionado y atómico en `data/settings.json` bajo la raíz de IgoonTube. En desarrollo la raíz es `F:\PUPlayer`; en la versión instalada o portable es la carpeta que contiene `IgoonTube.exe`. No se escribe en C:.

`AppSettings` contiene:

- `Version`: `1`.
- `Language`: `en` o `es`; `en` es el valor inicial cuando no existe configuración.
- `ThemePreset`: `CarbonBlue`, `MidnightSoft`, `GraphiteCompact` o `Custom`.
- `AccentColor`: color hexadecimal `#RRGGBB`.
- `ButtonCornerRadius`: entero entre 0 y 20.
- `ControlHeight`: entero entre 28 y 40.
- `Density`: `Compact` o `Comfortable`.
- `LastExportDirectory`: último destino local válido o `null`.

Un archivo ausente o corrupto carga Carbon Blue en inglés. La escritura usa `settings.json.tmp` y sustitución atómica. Cada proceso observa el archivo; los cambios realizados en una ventana llegan a las demás ventanas independientes en menos de un segundo. Los eventos propios se deduplican para evitar ciclos de escritura.

## Presets visuales

Carbon Blue es el preset inicial y conserva la identidad carbón, azul y blanco aprobada.

- `CarbonBlue`: `#2F8CFF`, radio 7, altura 32, densidad compacta.
- `MidnightSoft`: `#60A5FA`, radio 18, altura 34, densidad cómoda.
- `GraphiteCompact`: `#1683E8`, radio 3, altura 28, densidad compacta.

Seleccionar un preset aplica sus valores. Modificar color, radio, altura o densidad cambia `ThemePreset` a `Custom`. `Restaurar preset` reaplica los valores del preset seleccionado. Los cambios actualizan recursos WPF dinámicos sin reiniciar reproducción, posición, audio o seguimiento.

## Ajustes

La cabecera incorpora un botón compacto `Settings`/`Ajustes`. Su panel mantiene el estilo Carbon Blue y agrupa:

- idioma;
- preset visual;
- color principal;
- forma de botones;
- altura y densidad de controles;
- restauración del preset.

Los controles muestran una previsualización inmediata. Valores inválidos no se guardan y presentan un mensaje localizado. El panel es utilizable con teclado, tiene nombres accesibles y no ensancha la barra principal.

## Localización

Los textos visibles viven en diccionarios WPF `Strings.en.xaml` y `Strings.es.xaml`. Inglés es el diccionario inicial. El cambio de idioma sustituye el diccionario en ejecución y también localiza estados generados por los view models mediante claves estables, no texto duplicado.

Se traducen cabecera, controles, herramientas, audio, caché, análisis, exportación, mensajes de confirmación, errores controlados y estados. Nombres de archivos, rutas, nombres técnicos y salida externa de FFmpeg se conservan sin traducir.

## Exportación de clips

Antes de exportar se calcula una estimación conservadora:

- modo original rápido: tamaño proporcional del archivo fuente por duración, más 15 %;
- modo aplicar vista: estimación por duración a 12 Mb/s de video y 192 kb/s de audio, más 15 %.

La app comprueba `DriveInfo.AvailableFreeSpace` sobre el destino. Si no alcanza, no inicia FFmpeg y muestra el espacio requerido y disponible. `Exportar` abre un selector con el nombre incremental actual y MP4 como formato; cancelar no cambia estado ni crea archivos. El último directorio local elegido se recuerda globalmente; si deja de existir, la próxima exportación vuelve junto al video.

## Borrado de caché

Todas las acciones destructivas por video piden confirmación localizada e indican categoría y tamaño recuperable. Después del borrado informan bytes eliminados y archivos bloqueados que no pudieron quitarse. La limpieza global conserva su confirmación actual. Favoritos y archivos desconocidos nunca se eliminan.

## Firma y distribución

No se crea ni instala un certificado autofirmado. El instalador y ejecutables se publican sin firma. La entrega incluye hashes SHA-256 para verificar integridad y la documentación explica que Windows puede mostrar SmartScreen. La firma pública podrá añadirse posteriormente sin cambiar la aplicación.

## Pruebas

- Valores predeterminados, límites, presets, modo personalizado y JSON corrupto.
- Escritura atómica y propagación entre dos almacenes observando el mismo archivo.
- Inglés inicial y cambio bidireccional inglés/español.
- Recursos requeridos presentes en ambos idiomas.
- Estimación, espacio insuficiente, cancelación del selector y destino elegido.
- Confirmación y reporte de limpieza parcial con archivo bloqueado.
- Temas aplicados sin modificar el estado de dos paneles independientes.
- Suite Release completa, prueba del video real, recompilación portable e instalador sin firma.

## Fuera de alcance

- Descarga de temas o idiomas desde internet.
- Editor completo de estilos XAML.
- Certificado autofirmado, compra de certificado o publicación automática en GitHub.
- Más idiomas en esta versión.
