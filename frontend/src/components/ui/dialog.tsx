"use client";

import { useEffect, useRef, type ReactNode } from "react";
import { X } from "lucide-react";

const sizes = {
  md: "w-[min(32rem,calc(100vw-2rem))]",
  xl: "w-[min(76rem,calc(100vw-2rem))]",
};

type DialogProps = {
  open: boolean;
  title: string;
  size?: keyof typeof sizes;
  onClose: () => void;
  children: ReactNode;
};

/** Diálogo modal acessível baseado no elemento nativo <dialog> (foco preso, Esc fecha). */
export function Dialog({ open, title, size = "md", onClose, children }: DialogProps) {
  const dialogRef = useRef<HTMLDialogElement>(null);

  useEffect(() => {
    const dialog = dialogRef.current;
    if (!dialog) return;
    if (open && !dialog.open) dialog.showModal();
    if (!open && dialog.open) dialog.close();
  }, [open]);

  return (
    <dialog
      ref={dialogRef}
      onClose={onClose}
      onCancel={onClose}
      aria-label={title}
      className={`m-auto ${sizes[size]} rounded-2xl border bg-surface p-0 text-foreground shadow-xl`}
    >
      {open && (
        <div className="flex flex-col gap-4 p-6">
          <div className="flex items-start justify-between gap-4">
            <h2 className="text-lg font-semibold">{title}</h2>
            <button type="button" onClick={onClose} className="rounded-md p-1 text-muted-foreground hover:bg-surface-muted" aria-label="Fechar">
              <X className="size-5" aria-hidden />
            </button>
          </div>
          {children}
        </div>
      )}
    </dialog>
  );
}
