"use client";

import { useRef, useState, type KeyboardEvent, type PointerEvent } from "react";
import Image from "next/image";
import { ChevronLeft, ChevronRight, ImageOff } from "lucide-react";
import { cn } from "@/lib/cn";

const swipeThreshold = 40;

/** Carrossel de fotos com setas, teclado (← →), arraste no celular e miniaturas. */
export function ImageCarousel({ images, title }: { images: string[]; title: string }) {
  const [index, setIndex] = useState(0);
  const pointerStart = useRef<number | null>(null);

  if (images.length === 0) {
    return (
      <div className="flex aspect-[16/10] items-center justify-center rounded-2xl border bg-surface-muted text-muted-foreground">
        <ImageOff className="size-10" aria-hidden />
        <span className="sr-only">Imóvel sem fotos</span>
      </div>
    );
  }

  const go = (next: number) => setIndex((next + images.length) % images.length);

  function handleKeyDown(event: KeyboardEvent<HTMLDivElement>) {
    if (event.key === "ArrowLeft") go(index - 1);
    if (event.key === "ArrowRight") go(index + 1);
  }

  function handlePointerUp(event: PointerEvent<HTMLDivElement>) {
    if (pointerStart.current === null) return;
    const delta = event.clientX - pointerStart.current;
    pointerStart.current = null;
    if (Math.abs(delta) > swipeThreshold) go(delta < 0 ? index + 1 : index - 1);
  }

  return (
    <section aria-roledescription="carrossel" aria-label={`Fotos de ${title}`} className="flex flex-col gap-3">
      <div
        tabIndex={0}
        onKeyDown={handleKeyDown}
        onPointerDown={(event) => (pointerStart.current = event.clientX)}
        onPointerUp={handlePointerUp}
        className="relative aspect-[16/10] touch-pan-y select-none overflow-hidden rounded-2xl bg-surface-muted"
      >
        {images.map((image, imageIndex) => (
          <Image
            key={image}
            src={image}
            alt={`${title} - foto ${imageIndex + 1} de ${images.length}`}
            fill
            unoptimized
            preload={imageIndex === 0}
            sizes="(min-width: 1024px) 66vw, 100vw"
            draggable={false}
            className={cn("object-cover transition-opacity duration-300", imageIndex === index ? "opacity-100" : "opacity-0")}
            aria-hidden={imageIndex !== index}
          />
        ))}
        {images.length > 1 && (
          <>
            <button
              type="button"
              onClick={() => go(index - 1)}
              className="absolute left-3 top-1/2 flex size-10 -translate-y-1/2 items-center justify-center rounded-full bg-surface/90 text-foreground shadow transition hover:bg-surface"
              aria-label="Foto anterior"
            >
              <ChevronLeft className="size-5" aria-hidden />
            </button>
            <button
              type="button"
              onClick={() => go(index + 1)}
              className="absolute right-3 top-1/2 flex size-10 -translate-y-1/2 items-center justify-center rounded-full bg-surface/90 text-foreground shadow transition hover:bg-surface"
              aria-label="Próxima foto"
            >
              <ChevronRight className="size-5" aria-hidden />
            </button>
            <span className="absolute bottom-3 right-3 rounded-full bg-black/60 px-2.5 py-0.5 text-xs font-medium text-white" aria-live="polite">
              {index + 1} / {images.length}
            </span>
          </>
        )}
      </div>
      {images.length > 1 && (
        <div className="scrollbar-none flex gap-2 overflow-x-auto pb-1">
          {images.map((image, imageIndex) => (
            <button
              key={image}
              type="button"
              onClick={() => setIndex(imageIndex)}
              className={cn(
                "relative aspect-[4/3] w-24 shrink-0 overflow-hidden rounded-lg border-2 transition",
                imageIndex === index ? "border-accent" : "border-transparent opacity-70 hover:opacity-100",
              )}
              aria-label={`Ver foto ${imageIndex + 1}`}
              aria-current={imageIndex === index}
            >
              <Image src={image} alt="" fill unoptimized sizes="96px" className="object-cover" />
            </button>
          ))}
        </div>
      )}
    </section>
  );
}
