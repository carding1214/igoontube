# Prueba manual de seguimiento visual

## Resultado verificado

- Captura `screenshot-raw` real desde libmpv, RGB en memoria y ancho máximo de 384 px.
- MediaPipe 0.10.35 y Pose Landmarker Lite funcionan localmente en CPU.
- Video real indicado por el usuario: detección y estado `Siguiendo cara`.
- El zoom con rueda detuvo el seguimiento del panel y cambió el indicador a 1.2x.
- La pérdida de sujeto conserva el encuadre durante 2 s y después restablece el ajuste.
- Varias personas requieren clic; se elige el candidato más cercano.

## Comprobación manual

1. Activar `Seguir` y confirmar cuerpo completo, torso o cara según visibilidad.
2. Con varias personas, hacer clic en la deseada.
3. Ocultar temporalmente al sujeto y confirmar el restablecimiento a los 2 s.
4. Usar rueda o arrastre y confirmar `Seguimiento desactivado` solo en ese panel.
5. Repetir con dos paneles y ambas distribuciones.
6. Cerrar la ventana y confirmar que no quedan procesos Python ni workers.
