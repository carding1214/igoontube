# IgoonTube — herramientas de medios

## Objetivo

Añadir exportación de clips, favoritos rápidos, transformaciones visuales, miniaturas adaptativas y administración de caché sin modificar los videos originales ni saturar la barra compacta.

## Interfaz

Cada panel de video tendrá un botón compacto `Herramientas` con cuatro secciones:

- `Clip`: marcar inicio y fin, mostrar duración, elegir destino y exportar.
- `Imagen`: rotación de 0°, 90°, 180° o 270°, espejo horizontal/vertical y recorte rectangular manipulable sobre el video.
- `Favoritos`: añadir un marcador instantáneo, saltar a él o eliminarlo.
- `Caché`: mostrar el espacio usado por ese video y borrarlo.

El encabezado principal incluirá un administrador global que mostrará el tamaño total reconocido y permitirá borrar todas las cachés de IgoonTube. Al mover el puntero sobre la línea temporal aparecerá una miniatura del instante correspondiente.

## Exportación de clips

El usuario elegirá entre:

- `Original`: conserva la imagen original y usa corte híbrido.
- `Aplicar vista actual`: incorpora rotación, espejo y recorte; recodifica únicamente el clip seleccionado.

Para el corte híbrido, FFprobe localizará los fotogramas clave próximos a los límites. FFmpeg recodificará solo los grupos de imágenes de los extremos y copiará la sección central. Si el códec o contenedor no permite una unión fiable, la aplicación recodificará únicamente el clip seleccionado e informará del cambio de estrategia.

El audio se copiará sin pérdida cuando sea compatible. El formato predeterminado será MP4 y el archivo se guardará junto al original como `<nombre>_clip_001.mp4`. Los sufijos se incrementarán para evitar sobrescrituras.

## Transformaciones

Las transformaciones afectarán primero a la reproducción mediante las propiedades de libmpv. El recorte personalizado se seleccionará arrastrando y redimensionando un rectángulo sobre el video. La vista podrá restablecerse con una sola acción.

La exportación `Original` ignorará las transformaciones. `Aplicar vista actual` convertirá la selección normalizada a filtros FFmpeg y conservará la relación entre la previsualización y el archivo generado.

## Favoritos

La estrella añadirá un marcador en la posición actual sin abrir formularios. Los favoritos aparecerán sobre la línea temporal y dentro de la lista de herramientas. Se cargarán al abrir el video, independientemente de que el análisis de escenas se haya ejecutado.

Los favoritos se persistirán en un JSON pequeño dentro de `<video>.pucache`. El archivo incluirá identidad del video mediante tamaño y fecha de modificación para ignorar datos obsoletos.

## Miniaturas adaptativas

Las miniaturas se generarán progresivamente y bajo demanda. La cantidad objetivo se calculará a partir de la duración, con límites para impedir miles de imágenes: entre 60 y 300 muestras por video. Para un video de dos horas, el intervalo aproximado será de 40 segundos.

Se usarán JPEG pequeños y comprimidos. La miniatura más cercana se mostrará al desplazar el puntero por la línea temporal; si aún no existe, se generará en segundo plano sin bloquear la reproducción.

## Administración de caché

El administrador reconocerá únicamente manifiestos y archivos creados por IgoonTube: audio procesado, índices de escenas, favoritos y miniaturas. Nunca eliminará videos originales ni archivos desconocidos.

La limpieza por video y global mostrará el tamaño recuperable y pedirá confirmación. Las operaciones tolerarán archivos bloqueados, informarán lo que no pudo eliminarse y actualizarán el tamaño después de terminar.

## Robustez

- Se comprobará espacio libre antes de exportar y se mostrará una estimación.
- Las tareas mostrarán progreso y podrán cancelarse independientemente por panel.
- Las salidas incompletas usarán `.partial` y se eliminarán al cancelar o fallar.
- Una caché ausente o dañada se ignorará y podrá regenerarse.
- Solo habrá una exportación por panel, sin impedir la reproducción del otro video.
- El video original permanecerá intacto en todos los casos.

## Componentes

- `ClipExportService`: estrategia híbrida, alternativa de recodificación, nombres y progreso.
- `MediaTransform`: estado común para libmpv y filtros FFmpeg.
- `FavoriteStore`: persistencia y validación de favoritos.
- `ThumbnailService`: densidad, generación progresiva y lectura de miniaturas.
- `CacheManager`: descubrimiento seguro, cálculo de tamaño y limpieza.
- `PlayerPanelViewModel`: estado y cancelación independientes por video.
- `WorkspaceViewModel`: tamaño y limpieza globales.

## Verificación

Se cubrirán con pruebas automatizadas:

- límites sobre y entre fotogramas clave;
- unión híbrida y alternativa por códec incompatible;
- salida transformada y salida original;
- cancelación y eliminación de `.partial`;
- nombres sin sobrescritura;
- persistencia, carga y eliminación de favoritos;
- densidad adaptativa y reutilización de miniaturas;
- limpieza selectiva y global sin tocar archivos desconocidos;
- independencia entre dos paneles.

La prueba manual final usará el video indicado por el usuario y comprobará reproducción, previsualización, exportación, transformaciones, favoritos, miniaturas y limpieza.
