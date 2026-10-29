"use client";

import { useRef, useState, type ChangeEvent, type ReactNode } from "react";
import Image from "next/image";
import { ArrowLeft, ArrowRight, ImagePlus, Play, Star, Trash2 } from "lucide-react";
import { useAuth } from "@/components/admin/auth-provider";
import { Button } from "@/components/ui/button";
import { Alert, EmptyState } from "@/components/ui/feedback";
import { removePropertyMedia, reorderPropertyMedia, uploadPropertyMedia } from "@/lib/properties.api";
import type { Property } from "@/lib/properties.types";
import { assetUrl } from "@/lib/site-config";

const megabyte = 1024 * 1024;
const maxPhotoMegabytes = 10;
const maxVideoMegabytes = 200;
const maxFilesPerUpload = 10;
const maxMedia = 30;
const maxVideos = 5;
const acceptedTypes = "image/jpeg,image/png,image/webp,video/mp4,video/webm";

const isVideoFile = (file: File) => file.type.startsWith("video/");

type MediaManagerProps = {
  property: Property;
  readOnly: boolean;
  onChanged: (property: Property) => void;
};

/**
 * Fotos e vídeos do carrossel: envio, ordem, capa (primeira foto) e exclusão.
 * Vídeos entram no começo do carrossel; o corretor pode mudar a ordem com as setas.
 */
export function PropertyMediaManager({ property, readOnly, onChanged }: MediaManagerProps) {
  const { handleError } = useAuth();
  const inputRef = useRef<HTMLInputElement>(null);
  const [busy, setBusy] = useState(false);
  const [progress, setProgress] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const fileNames = property.media.map((item) => item.fileName);
  const videoCount = property.media.filter((item) => item.type === "Video").length;
  const coverIndex = property.media.findIndex((item) => item.type === "Photo");

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

  /** Cada vídeo vai numa requisição (a API aceita até 201 MB por envio); as fotos vão em lotes de até 10. */
  async function upload(files: File[]) {
    const videos = files.filter(isVideoFile);
    const photos = files.filter((file) => !isVideoFile(file));
    const batches = [...videos.map((video) => [video])];
    for (let start = 0; start < photos.length; start += maxFilesPerUpload) {
      batches.push(photos.slice(start, start + maxFilesPerUpload));
    }

    setBusy(true);
    setError(null);
    try {
      for (const [batchIndex, batch] of batches.entries()) {
        setProgress(batches.length > 1 ? `Enviando ${batchIndex + 1} de ${batches.length}...` : "Enviando...");
        onChanged(await uploadPropertyMedia(property.id, batch));
      }
    } catch (exception) {
      setError(handleError(exception));
    } finally {
      setBusy(false);
      setProgress(null);
    }
  }

  function handleFiles(event: ChangeEvent<HTMLInputElement>) {
    const files = [...(event.target.files ?? [])];
    event.target.value = "";
    if (files.length === 0) return;
    if (files.length + fileNames.length > maxMedia) return setError(`Cada imóvel pode ter no máximo ${maxMedia} fotos e vídeos.`);
    if (files.filter(isVideoFile).length + videoCount > maxVideos) return setError(`Cada imóvel pode ter no máximo ${maxVideos} vídeos.`);
    const tooLarge = files.find((file) => file.size > (isVideoFile(file) ? maxVideoMegabytes : maxPhotoMegabytes) * megabyte);
    if (tooLarge) {
      return setError(`O arquivo "${tooLarge.name}" passa de ${isVideoFile(tooLarge) ? maxVideoMegabytes : maxPhotoMegabytes} MB.`);
    }
    void upload(files);
  }

  function move(index: number, offset: number) {
    const reordered = [...fileNames];
    const [item] = reordered.splice(index, 1);
    reordered.splice(index + offset, 0, item);
    void run(() => reorderPropertyMedia(property.id, reordered));
  }

  return (
    <div className="flex flex-col gap-4">
      {property.media.length === 0 ? (
        <EmptyState title="Nenhuma foto ou vídeo ainda.">
          A primeira foto será a capa do anúncio. Vídeos aparecem primeiro no carrossel; a ordem pode ser mudada depois.
        </EmptyState>
      ) : (
        <ul className="grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-4">
          {property.media.map((item, index) => {
            const url = assetUrl(item.url) ?? "";
            const isVideo = item.type === "Video";
            return (
              <li key={item.fileName} className="group relative overflow-hidden rounded-xl border bg-surface-muted">
                <div className="relative aspect-[4/3] bg-black">
                  {isVideo ? (
                    <video src={url} muted playsInline preload="metadata" aria-label={`Vídeo ${index + 1}`} className="size-full object-cover" />
                  ) : (
                    <Image src={url} alt={`Foto ${index + 1}`} fill unoptimized sizes="200px" className="object-cover" />
                  )}
                </div>
                {index === coverIndex && (
                  <span className="absolute left-2 top-2 inline-flex items-center gap-1 rounded-full bg-gold px-2 py-0.5 text-xs font-semibold text-gold-foreground">
                    <Star className="size-3 fill-current" aria-hidden /> Capa
                  </span>
                )}
                {isVideo && (
                  <span className="absolute left-2 top-2 inline-flex items-center gap-1 rounded-full bg-black/70 px-2 py-0.5 text-xs font-semibold text-white">
                    <Play className="size-3 fill-current" aria-hidden /> Vídeo
                  </span>
                )}
                {!readOnly && (
                  <div className="flex items-center justify-between gap-1 border-t bg-surface p-1.5">
                    <div className="flex gap-1">
                      <IconButton label="Mover para a esquerda" disabled={busy || index === 0} onClick={() => move(index, -1)}>
                        <ArrowLeft className="size-4" aria-hidden />
                      </IconButton>
                      <IconButton label="Mover para a direita" disabled={busy || index === property.media.length - 1} onClick={() => move(index, 1)}>
                        <ArrowRight className="size-4" aria-hidden />
                      </IconButton>
                      {!isVideo && index > coverIndex && (
                        <IconButton label="Usar como capa" disabled={busy} onClick={() => move(index, coverIndex - index)}>
                          <Star className="size-4" aria-hidden />
                        </IconButton>
                      )}
                    </div>
                    <IconButton
                      label={isVideo ? "Excluir vídeo" : "Excluir foto"}
                      danger
                      disabled={busy}
                      onClick={() => void run(() => removePropertyMedia(property.id, item.fileName))}
                    >
                      <Trash2 className="size-4" aria-hidden />
                    </IconButton>
                  </div>
                )}
              </li>
            );
          })}
        </ul>
      )}

      {error && <Alert>{error}</Alert>}

      {!readOnly && (
        <div className="flex flex-wrap items-center gap-3">
          <input ref={inputRef} type="file" accept={acceptedTypes} multiple hidden onChange={handleFiles} />
          <Button variant="secondary" loading={busy} onClick={() => inputRef.current?.click()}>
            <ImagePlus className="size-4" aria-hidden /> {progress ?? "Adicionar fotos e vídeos"}
          </Button>
          <p className="text-xs text-muted-foreground">
            Fotos JPG, PNG ou WebP até {maxPhotoMegabytes} MB · vídeos MP4 ou WebM até {maxVideoMegabytes} MB (máx. {maxVideos}) ·{" "}
            {fileNames.length}/{maxMedia} arquivos
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
