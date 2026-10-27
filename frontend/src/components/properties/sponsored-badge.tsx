import { Star } from "lucide-react";
import { cn } from "@/lib/cn";

export function SponsoredBadge({ className }: { className?: string }) {
  return (
    <span
      className={cn(
        "inline-flex items-center gap-1 rounded-full bg-amber-400 px-2.5 py-0.5 text-xs font-semibold text-amber-950 shadow-sm",
        className,
      )}
    >
      <Star className="size-3.5 fill-current" aria-hidden />
      Destaque
    </span>
  );
}
