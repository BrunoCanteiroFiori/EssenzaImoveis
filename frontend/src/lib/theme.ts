export type ThemePreference = "light" | "dark" | "system";

export const themeStorageKey = "theme";

/**
 * Roda no <head> antes da primeira pintura: aplica a classe "dark" conforme a preferência salva
 * (ou a do sistema operacional), evitando o "flash" de tema errado.
 */
export const themeScript = `(function(){try{var t=localStorage.getItem("${themeStorageKey}");var d=t==="dark"||((!t||t==="system")&&window.matchMedia("(prefers-color-scheme: dark)").matches);document.documentElement.classList.toggle("dark",d)}catch(e){}})()`;

export function applyTheme(preference: ThemePreference) {
  const prefersDark = window.matchMedia("(prefers-color-scheme: dark)").matches;
  const dark = preference === "dark" || (preference === "system" && prefersDark);
  document.documentElement.classList.toggle("dark", dark);
}
