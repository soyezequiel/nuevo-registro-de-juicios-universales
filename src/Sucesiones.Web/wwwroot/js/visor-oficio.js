(function () {
  let scrollToken = 0;

  function scrollToPage(id, intentos = 0, token = scrollToken) {
    if (token !== scrollToken) return;

    const pagina = document.getElementById(id);
    if (!pagina) return;

    const contenedor = document.querySelector(".visor-modal main");
    if (!contenedor) return;
    contenedor.style.overflowAnchor = "none";

    let topDestino = 0;
    const paginas = contenedor.querySelectorAll("figure");
    for (const item of paginas) {
      if (item === pagina) break;

      const estilo = window.getComputedStyle(item);
      topDestino += item.offsetHeight + parseFloat(estilo.marginBottom || "0");
    }

    contenedor.scrollTop = topDestino;

    const imagenesPendientes = Array.from(contenedor.querySelectorAll("img"))
      .some((imagen) => !imagen.complete);

    if (intentos < 20 && (intentos < 5 || imagenesPendientes || Math.abs(contenedor.scrollTop - topDestino) > 2)) {
      window.setTimeout(() => scrollToPage(id, intentos + 1, token), 100);
    }
  }

  function markAsCurrent(boton) {
    const lista = boton.closest("[data-pagina-lista]");
    const botones = lista
      ? lista.querySelectorAll("[data-scroll-target]")
      : document.querySelectorAll("[data-scroll-target]");

    botones.forEach((item) => {
      item.removeAttribute("aria-current");
      item.classList.remove("border-2", "border-marca-azul");
      item.classList.add("border", "border-regla-suave");

      const label = item.querySelector("[data-pagina-label]");
      if (label) {
        label.classList.remove("text-marca-azul");
        label.classList.add("text-tinta");
      }
    });

    boton.setAttribute("aria-current", "page");
    boton.classList.remove("border", "border-regla-suave");
    boton.classList.add("border-2", "border-marca-azul");

    const label = boton.querySelector("[data-pagina-label]");
    if (label) {
      label.classList.remove("text-tinta");
      label.classList.add("text-marca-azul");
    }
  }

  function selectPage(boton) {
    scrollToken += 1;
    markAsCurrent(boton);
    scrollToPage(boton.dataset.scrollTarget, 0, scrollToken);
  }

  document.addEventListener("click", (evento) => {
    const boton = evento.target.closest("[data-scroll-target]");
    if (!boton) return;

    evento.preventDefault();
    selectPage(boton);
  });

  window.sucesionesVisor = { scrollToPage, selectPage };
})();
