(() => {
  "use strict";
  const root = document.documentElement;
  const themeRadios = [...document.querySelectorAll('input[name="theme"]')];
  let saved = "system";
  try { saved = localStorage.getItem("aural-preview-theme") || "system"; } catch {}
  if (!["system", "light", "dark"].includes(saved)) saved = "system";
  function applyTheme(value) {
    root.dataset.theme = value;
    for (const radio of themeRadios) radio.checked = radio.value === value;
    try { localStorage.setItem("aural-preview-theme", value); } catch {}
  }
  applyTheme(saved);
  for (const radio of themeRadios) radio.addEventListener("change", () => { if (radio.checked) applyTheme(radio.value); });

  const az = document.getElementById("azimuth");
  const el = document.getElementById("elevation");
  const dist = document.getElementById("distance");
  const dot = document.getElementById("source-dot");
  const status = document.getElementById("feedback");
  const radians = Math.PI / 180;
  function render() {
    const a = Number(az.value);
    const e = Number(el.value);
    const d = Number(dist.value);
    document.getElementById("azimuth-value").textContent = a + "°";
    document.getElementById("elevation-value").textContent = e + "°";
    document.getElementById("distance-value").textContent = d.toFixed(1) + "×";
    const radius = 13 + (d - 1) * 13;
    const x = Math.sin(a * radians) * radius;
    const y = -Math.cos(a * radians) * radius + (e / 45) * 9;
    dot.style.left = (50 + x) + "%";
    dot.style.top = (50 + y) + "%";
  }
  for (const slider of [az, el, dist]) slider.addEventListener("input", () => {
    render();
    status.textContent = "Visual source updated. No sound or experiment action occurred.";
  });
  document.getElementById("reset").addEventListener("click", () => {
    az.value = "0"; el.value = "0"; dist.value = "1";
    render();
    status.textContent = "Visual preview reset. No sound was playing.";
  });
  window.addEventListener("blur", () => {
    status.textContent = "Window focus changed. No audio playback or remote controls exist.";
  });
  render();
  if ("serviceWorker" in navigator && (location.protocol === "https:" || ["localhost", "127.0.0.1"].includes(location.hostname))) {
    window.addEventListener("load", () => navigator.serviceWorker.register("./sw.js", {scope:"./"}).catch(() => {}));
  }
})();