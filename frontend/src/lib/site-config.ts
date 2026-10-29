export const siteConfig = {
  name: process.env.NEXT_PUBLIC_SITE_NAME ?? "Corretora de Imóveis",
  url: process.env.NEXT_PUBLIC_SITE_URL ?? "http://localhost:3000",
  apiUrl: process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:4000",
  whatsappNumber: (process.env.NEXT_PUBLIC_WHATSAPP_NUMBER ?? "").replace(/\D/g, ""),
};

/** URL absoluta de um arquivo servido pela API (ex.: fotos e vídeos em /properties/{id}/media/...). */
export function assetUrl(path: string | null | undefined): string | null {
  if (!path) return null;
  return path.startsWith("http") ? path : `${siteConfig.apiUrl}${path}`;
}
