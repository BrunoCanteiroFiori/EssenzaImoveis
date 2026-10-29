"use client";

import { useEffect, useRef, useState, type KeyboardEvent, type PointerEvent } from "react";
import Image from "next/image";
import { ChevronLeft, ChevronRight, ImageOff, Play } from "lucide-react";
import { cn } from "@/lib/cn";
import type { PropertyMediaType } from "@/lib/properties.types";

const swipeThreshold = 40;

export type CarouselItem = { url: string; type: PropertyMediaType };

/**
 * Carrossel de fotos e vídeos com setas, teclado (← →), arraste no celular e miniaturas.
 * Ao trocar de item, o vídeo que estava tocando é pausado.
 */
export function MediaCarousel({ items, title }: { items: CarouselItem[]; title: string }) {
  const [index, setIndex] = useState(0);
  const pointerStart = useRef<number | null>(null);
  const videos = useRef<(HTMLVideoElement | null)[]>([]);

  useEffect(() => {
    videos.current.forEach((video, videoIndex) => {
      if (videoIndex !== index) video?.pause();
    });
  }, [index]);

  if (items.length === 0) {
    return (
      <div className="flex aspect-[16/10] items-center justify-center rounded-2xl border bg-surface-muted text-muted-foreground">
        <ImageOff className="size-10" aria-hidden />
        <span className="sr-only">Imóvel sem fotos</span>
      </div>
    );
  }

  const go = (next: number) => setIndex((next + items.length) % items.length);

  function handleKeyDown(event: KeyboardEvent<HTMLDivElement>) {
    // No vídeo, as setas avançam e voltam o próprio vídeo.
    if (event.target instanceof HTMLVideoElement) return;
    if (event.key === "ArrowLeft") go(index - 1);
    if (event.key === "ArrowRight") go(index + 1);
  }

  function handlePointerDown(event: PointerEvent<HTMLDivElement>) {
    // Arrastar sobre o vídeo mexe na barra de progresso, não troca de item.
    pointerStart.current = event.target instanceof HTMLVideoElement ? null : event.clientX;
  }

  function handlePointerUp(event: PointerEvent<HTMLDivElement>) {
    if (pointerStart.current === null) return;
    const delta = event.clientX - pointerStart.current;
    pointerStart.current = null;
    if (Math.abs(delta) > swipeThreshold) go(delta < 0 ? index + 1 : index - 1);
  }

  return (
    <section aria-roledescription="carrossel" aria-label={`Fotos e vídeos de ${title}`} className="flex flex-col gap-3">
      <div
        tabIndex={0}
        onKeyDown={handleKeyDown}
        onPointerDown={handlePointerDown}
        onPointerUp={handlePointerUp}
        className="relative aspect-[16/10] touch-pan-y select-none overflow-hidden rounded-2xl bg-surface-muted"
      >
        {items.map((item, itemIndex) => {
          const active = itemIndex === index;
          const label = `${title} - ${item.type === "Video" ? "vídeo" : "foto"} ${itemIndex + 1} de ${items.length}`;
          const className = cn(
            "transition-opacity duration-300",
            active ? "opacity-100" : "pointer-events-none opacity-0",
          );
          return item.type === "Video" ? (
            <video
              key={item.url}
              ref={(element) => {
                videos.current[itemIndex] = element;
              }}
              src={item.url}
              controls
              playsInline
              preload="metadata"
              aria-label={label}
              aria-hidden={!active}
              tabIndex={active ? 0 : -1}
              className={cn("absolute inset-0 size-full bg-black object-contain", className)}
            />
          ) : (
            <Image
              key={item.url}
              src={item.url}
              alt={label}
              fill
              unoptimized
              preload={itemIndex === 0}
              sizes="(min-width: 1024px) 66vw, 100vw"
              draggable={false}
              className={cn("object-cover", className)}
              aria-hidden={!active}
            />
          );
        })}
        {items.length > 1 && (
          <>
            <button
              type="button"
              onClick={() => go(index - 1)}
              className="absolute left-3 top-1/2 flex size-10 -translate-y-1/2 items-center justify-center rounded-full bg-surface/90 text-foreground shadow transition hover:bg-surface"
              aria-label="Anterior"
            >
              <ChevronLeft className="size-5" aria-hidden />
            </button>
            <button
              type="button"
              onClick={() => go(index + 1)}
              className="absolute right-3 top-1/2 flex size-10 -translate-y-1/2 items-center justify-center rounded-full bg-surface/90 text-foreground shadow transition hover:bg-surface"
              aria-label="Próximo"
            >
              <ChevronRight className="size-5" aria-hidden />
            </button>
            <span className="absolute right-3 top-3 rounded-full bg-black/60 px-2.5 py-0.5 text-xs font-medium text-white" aria-live="polite">
              {index + 1} / {items.length}
            </span>
          </>
        )}
      </div>
      {items.length > 1 && (
        <div className="scrollbar-none flex gap-2 overflow-x-auto pb-1">
          {items.map((item, itemIndex) => (
            <button
              key={item.url}
              type="button"
              onClick={() => setIndex(itemIndex)}
              className={cn(
                "relative aspect-[4/3] w-24 shrink-0 overflow-hidden rounded-lg border-2 bg-black transition",
                itemIndex === index ? "border-accent" : "border-transparent opacity-70 hover:opacity-100",
              )}
              aria-label={`Ver ${item.type === "Video" ? "vídeo" : "foto"} ${itemIndex + 1}`}
              aria-current={itemIndex === index}
            >
              {item.type === "Video" ? (
                <>
                  <video src={item.url} muted playsInline preload="metadata" className="pointer-events-none size-full object-cover" />
                  <Play className="absolute inset-0 m-auto size-6 fill-white text-white drop-shadow" aria-hidden />
                </>
              ) : (
                <Image src={item.url} alt="" fill unoptimized sizes="96px" className="object-cover" />
              )}
            </button>
          ))}
        </div>
      )}
    </section>
  );
}
