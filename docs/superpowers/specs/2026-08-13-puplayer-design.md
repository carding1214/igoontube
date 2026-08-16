# PUPlayer — Diseño

## Objetivo

Crear un reproductor privado para Windows que abra archivos locales en ventanas independientes o en un mosaico de hasta dos videos, permita controlar, ampliar y seguir sujetos por panel, mejore el audio en tiempo real y ofrezca separación de voces mediante IA local con caché reutilizable.

## Alcance

- Windows 10/11 x64.
- Archivos locales; sin URLs ni streaming.
- Sin lanzador: doble clic, `Enter` sobre varios archivos o “Abrir con”.
- Reproducción simultánea sin instancia única.
- Arrastrar un segundo archivo a una ventana activa crea un mosaico; no se admiten más de dos paneles.
- Sin historial, miniaturas, telemetría ni conexiones de red.
- El instalador registra formatos compatibles únicamente con consentimiento del usuario.

## Arquitectura

- Aplicación WPF sobre .NET 8.
- mpv para reproducción, compatibilidad de formatos, audio externo, zoom y paneo.
- Cada invocación crea un proceso WPF sin instancia única y un proceso mpv embebido por panel.
- WPF controla cada mpv mediante una tubería con nombre local y JSON IPC; mpv se inicia sin configuración externa, historial ni archivos de reanudación.
- En modo mosaico, cada panel mantiene proceso mpv, reproducción, posición, audio, zoom y estado propios.
- FFmpeg extrae y codifica audio sin recodificar ni duplicar el video.
- Un coordinador local de IA atiende separación de audio y seguimiento visual sin duplicar modelos por ventana.
- El seguimiento visual tiene prioridad interactiva; solo se ejecuta una separación de audio a la vez para limitar memoria, GPU y CPU.
- Proyecto, dependencias, temporales de procesamiento e instalador viven bajo `F:\PUPlayer` para no ocupar C.

## Interfaz

- Estilo moderno oscuro, correspondiente al mockup B.
- Video como contenido principal y panel lateral de audio plegable.
- Cada panel incluye barra de tiempo, reproducción, saltos de ±10 segundos, velocidad, tiempo, volumen, ajustes de audio, zoom y seguimiento.
- Las barras y atajos actúan solo sobre su panel; no existe control ni sincronización maestra.
- Cada ventana conserva ajustes propios mientras permanece abierta.
- Los presets personalizados pueden guardarse globalmente sin asociarlos a nombres de archivos.

## Mosaico

- Doble clic abre un video en una ventana normal.
- Arrastrar otro archivo sobre esa ventana crea dos paneles y comienza su reproducción independiente.
- La distribución inicial es mitad y mitad horizontal.
- Un botón alterna entre mitad y mitad horizontal y arriba/abajo sin reiniciar los reproductores.
- Redimensionar la ventana o entrar a pantalla completa ajusta ambos paneles al espacio disponible.
- Cerrar un panel expande el restante automáticamente.
- Arrastrar un tercer archivo solicita elegir cuál panel reemplazar.

## Zoom y seguimiento de sujeto

- La rueda ajusta el zoom alrededor de la posición del cursor.
- El rango va desde ajuste completo hasta 8× por panel.
- Arrastrar desplaza el encuadre; los límites evitan mostrar espacio vacío fuera del video.
- Doble clic restablece zoom, paneo y ajuste automático al panel.
- El encuadre se recalcula al redimensionar o alternar la distribución sin perder la región seleccionada.
- “Seguir sujeto” analiza el video localmente y muestra candidatos; el usuario elige uno con un clic.
- El encuadre conserva el cuerpo completo cuando es visible, añade margen y usa torso o cara cuando el cuerpo sale del cuadro.
- La detección usa fotogramas de baja resolución a un objetivo de 5 FPS y suaviza posición y escala; el video visible mantiene su resolución completa.
- Si pierde el sujeto durante dos segundos, conserva brevemente el último encuadre y después vuelve a ajuste completo para permitir otra selección.
- El modo no reconoce identidades ni guarda fotogramas.
- Usar zoom o paneo manual pausa el seguimiento solo en ese panel.

## Audio inmediato

- Volumen de 0 a 200% por ventana.
- En mosaico, volumen y filtros son independientes por panel y ambos audios pueden sonar simultáneamente.
- Presets: Natural, Voz clara, ASMR, Reducir música, Reducir ruido y Personalizado.
- Ajustes manuales: ecualizador, ganancia, compresor, reducción de ruido y balance.
- Los filtros operan en tiempo real y no modifican el archivo original.

## Separación de voces con IA

- El comando “Aislar voces” procesa únicamente el audio en el equipo local.
- El video puede continuar mientras se muestra progreso y opción de cancelar.
- Cuando termina, el reproductor cambia al audio procesado conservando la posición.
- La IA resalta vocalizaciones humanas; no clasifica escenas ni determina qué contenido es “interesante”.
- El modelo se incluye con la instalación y funciona sin internet.

## Caché de audio

- La caché persistente contiene solo audio procesado; nunca duplica el flujo de video.
- Se almacena en una carpeta oculta `.pucache` junto al archivo original.
- El formato será Matroska Audio `.mka` con Opus VBR optimizado para voz: 96 kbps estéreo o 64 kbps mono.
- Como referencia, 2 horas a 96 kbps ocupan aproximadamente 86 MB, frente a un video original de varios GB.
- El reproductor carga la caché como pista de audio externa y mantiene el video original sin cambios.
- Un manifiesto JSON pequeño registra tamaño, fecha de modificación y SHA-256 de los primeros y últimos 64 KiB; la caché se identifica por nombre y esa huella.
- Si el original cambia, la caché se considera inválida y se regenera.
- Los archivos intermedios sin comprimir se crean bajo `F:\PUPlayer\data\temp` y se eliminan al finalizar, cancelar o iniciar de nuevo tras un cierre inesperado.
- La interfaz ofrece “Eliminar audio procesado”.
- Si la carpeta del video no permite escritura, la aplicación pide una ubicación alternativa; no escribe silenciosamente en C.
- Si el video está en C, la aplicación avisa del espacio requerido y permite guardar esa caché en `F:\PUPlayer\data\cache`.

## Privacidad

- No se guardan archivos recientes, posiciones, miniaturas ni listas de reproducción.
- No se envían nombres, metadatos, audio ni video a internet.
- La comunicación interna usa únicamente procesos y tuberías locales.
- Los fotogramas reducidos usados para seguimiento permanecen en memoria y no se guardan.
- Solo persisten preferencias globales y cachés de audio solicitadas explícitamente.
- Los registros técnicos omiten rutas y nombres de medios.

## Errores

- Formato no compatible: mensaje breve sin crear historial.
- Espacio insuficiente: cálculo previo y cancelación segura.
- Caché corrupta o incompleta: descarte y regeneración.
- Falta de GPU: cambio automático a CPU.
- Fallo del trabajador de IA: la reproducción original continúa.
- Fallo de un mpv: solo se detiene su panel y puede reiniciarse; otros paneles y ventanas permanecen activos.
- Seguimiento no disponible o sujeto perdido: se conserva zoom manual y ajuste completo.
- Tercer archivo en mosaico: se exige elegir un panel antes de reemplazarlo.

## Rendimiento

- mpv usa decodificación por hardware cuando el equipo la admite.
- El modelo visual se carga solo al activar “Seguir sujeto”.
- El análisis visual se limita a dos paneles y fotogramas reducidos; si detecta saturación, baja gradualmente de 5 a 2 FPS.
- El coordinador comparte modelos y prioriza seguimiento sobre separación de audio.
- Alternar distribución modifica únicamente el diseño WPF y conserva posición, zoom, audio y procesos mpv.

## Validación

- Pruebas unitarias de presets, límites de volumen, claves de caché y limpieza.
- Pruebas unitarias de transformaciones de zoom, paneo, ajuste, suavizado y selección de sujeto.
- Pruebas de integración de mpv IPC, FFmpeg, coordinador de IA y audio externo sincronizado.
- Pruebas de procesos múltiples, asociaciones de archivos y aislamiento de fallos.
- Pruebas de dos barras de tiempo independientes, mezcla simultánea de audio y cambio de distribución sin reinicio.
- Pruebas de seguimiento con una y varias personas, pérdida de objetivo y retorno a ajuste completo.
- Pruebas manuales con MP4, MKV, WebM, AVI, MP3, FLAC y archivos largos.
- Verificación de que proyecto, modelos, temporales y cachés alternativas permanecen en F, y de que no existen conexiones de red durante reproducción o procesamiento.

## Criterios de aceptación

1. Abrir varios archivos desde el Explorador crea procesos y ventanas independientes.
2. Arrastrar un segundo video crea un mosaico y permite alternar distribución horizontal o vertical.
3. Cada panel reproduce, busca, cambia velocidad y ajusta su audio sin afectar al otro.
4. El zoom manual funciona alrededor del cursor y conserva el encuadre al redimensionar.
5. El seguimiento permite elegir una persona y encuadra cuerpo, torso o cara sin guardar imágenes.
6. Ambos audios pueden sonar simultáneamente con volumen, filtros y presets separados.
7. El volumen llega a 200% con protección contra saturación mediante compresión.
8. Los presets inmediatos funcionan sin procesar el archivo.
9. “Aislar voces” funciona completamente en local, se puede cancelar y reutiliza la caché.
10. La caché contiene solo audio optimizado y no duplica el video.
11. No se guarda historial ni se transmite información.
12. Proyecto, instalador y temporales propios permanecen en F.
