const currency = new Intl.NumberFormat("pt-BR", { style: "currency", currency: "BRL", maximumFractionDigits: 0 });
const decimal = new Intl.NumberFormat("pt-BR", { maximumFractionDigits: 2 });
const dateTime = new Intl.DateTimeFormat("pt-BR", { dateStyle: "short", timeStyle: "short", timeZone: "America/Sao_Paulo" });

export const formatPrice = (value: number) => currency.format(value);

export const formatArea = (value: number) => `${decimal.format(value)} m²`;

export const formatDateTime = (value: string) => dateTime.format(new Date(value));

export function formatPhone(digits: string) {
  const local = digits.length > 11 ? digits.slice(-11) : digits;
  if (local.length === 11) return `(${local.slice(0, 2)}) ${local.slice(2, 7)}-${local.slice(7)}`;
  if (local.length === 10) return `(${local.slice(0, 2)}) ${local.slice(2, 6)}-${local.slice(6)}`;
  return digits;
}

/** Máscara de CEP (00000-000) para o que foi digitado ou colado: descarta o que não é dígito e o excesso. */
export function formatZipCode(value: string) {
  const digits = value.replace(/\D/g, "").slice(0, 8);
  return digits.length > 5 ? `${digits.slice(0, 5)}-${digits.slice(5)}` : digits;
}

/** Máscara de valor com centavos (1.234,56): os dígitos entram pela direita, como em caixa eletrônico. */
export function formatDecimalInput(value: string) {
  const digits = value.replace(/\D/g, "").replace(/^0+/, "").slice(0, 11);
  if (digits === "") return "";
  const padded = digits.padStart(3, "0");
  return `${padded.slice(0, -2).replace(/\B(?=(\d{3})+$)/g, ".")},${padded.slice(-2)}`;
}

/** Número digitado na máscara de formatDecimalInput (null se vazia). */
export function parseDecimalInput(value: string) {
  const digits = value.replace(/\D/g, "");
  return digits === "" ? null : Number(digits) / 100;
}

/** Máscara de contagem (quartos, banheiros, vagas): até dois dígitos, sem zero à esquerda. */
export function formatCountInput(value: string) {
  return value.replace(/\D/g, "").replace(/^0+(?=\d)/, "").slice(0, 2);
}

export function pluralize(count: number, singular: string, plural: string) {
  return `${count} ${count === 1 ? singular : plural}`;
}
