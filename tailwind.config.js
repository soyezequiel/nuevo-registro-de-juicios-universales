// [BUSCAR] Estructura de config de Tailwind. Sale del tutorial oficial.
// El JSDoc de arriba habilita autocomplete si el editor entiende TypeScript.

/** @type {import('tailwindcss').Config} */
module.exports = {
  // [BUSCAR] `content` = lista de archivos que Tailwind escanea para detectar qué
  // clases usás. Las clases que no aparecen acá NO se incluyen en el CSS final
  // (es lo que mantiene salida.css chico). Si agregás carpetas con .razor o .cshtml
  // fuera de estos paths, hay que sumarlas o las clases se borran.
  content: [
    "./src/Sucesiones.Web/Pages/**/*.cshtml",
    "./src/Sucesiones.Web/Componentes/**/*.razor",
    "./src/Sucesiones.Web/**/*.razor",
  ],
  theme: {
    // [BUSCAR] `extend` agrega valores al theme default sin reemplazarlo.
    // Si usaras `theme.colors` directo (sin extend), perdés los colores built-in.
    // [ESTILO] Agrupo los colores propios bajo `marca` para que se vean como
    // `bg-marca-azul` en lugar de chocar con nombres genéricos como `blue`.
    extend: {
      colors: {
        marca: {
          azul: "#1e3a8a",
          azulClaro: "#3b82f6",
          negro: "#0a0a0a",
        },
      },
      fontFamily: {
        sans: ["Inter", "system-ui", "sans-serif"],
      },
    },
  },
  plugins: [],
};
