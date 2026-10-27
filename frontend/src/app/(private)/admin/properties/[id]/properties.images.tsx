"use client";

import { useRef, useState, type ChangeEvent, type ReactNode } from "react";
import Image from "next/image";
import { ArrowLeft, ArrowRight, ImagePlus, Star, Trash2 } from "lucide-react";
import { useAuth } from "@/components/admin/auth-provider";
import { Button } from "@/components/ui/button";
import { Alert, EmptyState } from "@/components/ui/feedback";
import { removePropertyImage, reorderPropertyImages, uploadPropertyImages } from "@/lib/properties.api";
import type { Property } from "@/lib/properties.types";
import { assetUrl } from "@/lib/site-config";

const maxFileBytes = 10 * 1024 * 1024;
const maxFilesPerUpload = 10;
const maxImages = 30;

type ImagesManagerProps = {
  property: Property;
  readOnly: boolean;
  onChanged: (property: Property) => void;
};

/** Fotos do carrossel: envio (JPG, PNG ou WebP), ordem, capa (primeira foto) e exclusão. */
export function PropertyImagesManager({ property, readOnly, onChanged }: ImagesManagerProps) {
  const { handleError } = useAuth();
  const inputRef = useRef<HTMLInputElement>(null);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const fileNames = property.images.map((image) => image.fileName);

  async function run(action: () => Promise<Property>) {
    setBusy(true);
    setError(null);
    try {
      onChanged(await action());
    } catch (exception) {
      setError(handleError(exception));
    } finally {
      setBusy(false);
    }
  }

  function handleFiles(event: ChangeEvent<HTMLInputElement>) {
    const files = [...(event.target.files ?? [])];
    event.target.value = "";
    if (files.length === 0) return;
    if (files.length > maxFilesPerUpload) return setError(`Envie no máximo ${maxFilesPerUpload} fotos por vez.`);
    if (files.length + fileNames.length > maxImages) return setError(`Cada imóvel pode ter no máximo ${maxImages} fotos.`);
    const tooLarge = files.find((file) => file.size > maxFileBytes);
    if (tooLarge) return setError(`A foto "${tooLarge.name}" passa de 10 MB.`);
    void run(() => uploadPropertyImages(property.id, files));
  }

  function move(index: number, offset: number) {
    const reordered = [...fileNames];
    const [item] = reordered.splice(index, 1);
    reordered.splice(index + offset, 0, item);
    void run(() => reorderPropertyImages(property.id, reordered));
  }

  return (
    <div className="flex flex-col gap-4">
      {property.images.length === 0 ? (
        <EmptyState title="Nenhuma foto ainda.">A primeira foto enviada será a capa do anúncio.</EmptyState>
      ) : (
        <ul className="grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-4">
          {property.images.map((image, index) => (
            <li key={image.fileName} className="group relative overflow-hidden rounded-xl border bg-surface-muted">
              <div className="relative aspect-[4/3]">
                <Image src={assetUrl(image.url) ?? ""} alt={`Foto ${index + 1}`} fill unoptimized sizes="200px" className="object-cover" />
              </div>
              {index === 0 && (
                <span className="absolute left-2 top-2 inline-flex items-center gap-1 rounded-full bg-amber-400 px-2 py-0.5 text-xs font-semibold text-amber-950">
                  <Star className="size-3 fill-current" aria-hidden /> Capa
                </span>
              )}
              {!readOnly && (
                <div className="flex items-center justify-between gap-1 border-t bg-surface p-1.5">
                  <div className="flex gap-1">
                    <IconButton label="Mover para a esquerda" disabled={busy || index === 0} onClick={() => move(index, -1)}>
                      <ArrowLeft className="size-4" aria-hidden />
                    </IconButton>
                    <IconButton label="Mover para a direita" disabled={busy || index === property.images.length - 1} onClick={() => move(index, 1)}>
                      <ArrowRight className="size-4" aria-hidden />
                    </IconButton>
                    {index > 0 && (
                      <IconButton label="Usar como capa" disabled={busy} onClick={() => move(index, -index)}>
                        <Star className="size-4" aria-hidden />
                      </IconButton>
                    )}
                  </div>
                  <IconButton
                    label="Excluir foto"
                    danger
                    disabled={busy}
                    onClick={() => void run(() => removePropertyImage(property.id, image.fileName))}
                  >
                    <Trash2 className="size-4" aria-hidden />
                  </IconButton>
                </div>
              )}
            </li>
          ))}
        </ul>
      )}

      {error && <Alert>{error}</Alert>}

      {!readOnly && (
        <div className="flex flex-wrap items-center gap-3">
          <input ref={inputRef} type="file" accept="image/jpeg,image/png,image/webp" multiple hidden onChange={handleFiles} />
          <Button variant="secondary" loading={busy} onClick={() => inputRef.current?.click()}>
            <ImagePlus className="size-4" aria-hidden /> Adicionar fotos
          </Button>
          <p className="text-xs text-muted-foreground">
            JPG, PNG ou WebP, até 10 MB cada · {fileNames.length}/{maxImages} fotos
          </p>
        </div>
      )}
    </div>
  );
}

function IconButton({
  label,
  danger,
  disabled,
  onClick,
  children,
}: {
  label: string;
  danger?: boolean;
  disabled?: boolean;
  onClick: () => void;
  children: ReactNode;
}) {
  return (
    <button
      type="button"
      title={label}
      aria-label={label}
      disabled={disabled}
      onClick={onClick}
      className={`rounded-md p-1.5 transition-colors disabled:opacity-30 ${danger ? "text-danger hover:bg-danger-soft" : "text-muted-foreground hover:bg-surface-muted hover:text-foreground"}`}
    >
      {children}
    </button>
  );
}
