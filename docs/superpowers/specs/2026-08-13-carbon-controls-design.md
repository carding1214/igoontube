# PUPlayer — controles carbón pulidos

## Objetivo

Refinar la interfaz WPF existente con controles compactos, claros y consistentes. El video seguirá dominando la ventana y no cambiará ninguna función de reproducción, audio, zoom ni mosaico.

## Dirección visual

- Fondo negro y superficies carbón en capas.
- Ámbar suave reservado para foco, selección y datos activos.
- Controles redondeados con borde grafito; sin sombras decorativas.
- Tipografía Segoe UI, compacta y legible.
- Iconos vectoriales simples para reproducción, salto, distribución y cierre.

## Componentes

Los estilos reutilizables vivirán en recursos compartidos de la aplicación: botones normales, botones de icono, botón primario, ComboBox, Slider, CheckBox, Expander y texto secundario. Cada estado tendrá apariencia explícita: reposo, hover, pulsado, foco y desactivado.

La barra global será compacta y mostrará la distribución actual. Cada panel conservará encabezado, video y controles independientes. La línea de tiempo ocupará el ancho disponible; reproducción, volumen, velocidad y preset tendrán jerarquía clara. Los ajustes manuales de audio permanecerán plegados por defecto dentro de una superficie carbón delimitada.

## Comportamiento y datos

Solo cambia la presentación XAML. Los eventos, bindings, procesos independientes, reproducción simultánea y protocolo del worker permanecen intactos. Los nombres largos se recortarán sin desplazar acciones importantes. Los errores conservarán contraste y espacio propios.

## Accesibilidad

- Objetivos interactivos de al menos 34 px.
- Foco de teclado visible en ámbar.
- Tooltips y nombres comprensibles en botones de icono.
- Contraste legible para texto principal, secundario y estados desactivados.
- Orden de tabulación equivalente al orden visual.

## Verificación

Se ejecutará la suite completa y una revisión visual en una ventana real con uno y dos videos, ambas distribuciones, panel de audio abierto, nombres largos, hover, foco y estado desactivado. La revisión visual tendrá una pasada de corrección y una confirmación final como máximo.

## Fuera de alcance

No se añaden animaciones, historial, miniaturas, red, cambios de reproducción ni nuevas funciones de audio.
