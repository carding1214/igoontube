# Diseño de marca e interfaz IgoonTube

## Objetivo

Renombrar PUPlayer como `IgoonTube`, aplicar el logo elegido y modernizar la interfaz sin alterar reproducción, mosaico, seguimiento ni audio independiente.

## Marca

El logo usa una lente circular atravesada por una onda horizontal y un Play central. La palabra se escribe exactamente `IgoonTube`: `I` y `Tube` en blanco frío; `goon` en azul hielo. Se producirán PNG e ICO multirresolución para ventana, ejecutable, asociaciones e instalador.

## Paleta

- Fondo carbón: `#17191D`
- Superficie grafito: `#25282E`
- Texto blanco frío: `#F1F5F7`
- Acción azul hielo: `#69D2E7`
- Texto secundario: `#AEB7BE`

El azul identifica selección, progreso y acciones activas. El blanco se reserva para contenido principal y controles neutrales.

## Interfaz de ventana

La cabecera será compacta e incluirá logo, nombre y controles de distribución. Cada video conservará una barra independiente en una sola línea. Al reducir el ancho, las acciones secundarias pasan al menú existente y la barra nunca se divide en dos filas. Botones, selectores y estados usarán espaciado uniforme, bordes suaves y foco visible.

## Pantalla completa

Un doble clic sobre un panel muestra únicamente ese video en pantalla completa real sobre el monitor actual. El otro panel conserva reproducción y audio. Una barra inferior estilo VLC aparece al mover el mouse y se oculta después de dos segundos sin movimiento. El cursor también se oculta. Otro doble clic o `Esc` restaura exactamente la ventana, distribución y foco anteriores.

La barra contiene reproducción, tiempo, búsqueda, volumen, audio y salida de pantalla completa del panel seleccionado. No se añaden controles flotantes al modo ventana.

## Nombre y distribución

La aplicación visible, ejecutable, títulos, accesos, asociaciones, instalador y carpeta de distribución usarán `IgoonTube`. Los namespaces internos pueden conservar `PUPlayer` para evitar una migración sin beneficio funcional. El instalador seguirá usando F: por defecto y mantendrá la asociación opcional.

## Errores y estado

Si no hay video, el doble clic no entra en pantalla completa. Cerrar la ventana o perder el panel cancela el temporizador de ocultación. Ningún cambio de modo reinicia posición, zoom, seguimiento, preset, caché IA o volumen.

## Verificación

- Pruebas automatizadas del estado de pantalla completa y temporizador.
- Compilación Release y pruebas existentes.
- Prueba manual con uno y dos videos: entrada/salida, `Esc`, doble clic, búsqueda, barra automática, ambos monitores y audio independiente.
- Verificación visual de logo, colores, espaciado y ausencia de saltos de línea.
- Reconstrucción y prueba del portable e instalador renombrados.
