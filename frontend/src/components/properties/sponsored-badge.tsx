import { Star } from "lucide-react";
import { cn } from "@/lib/cn";

export function SponsoredBadge({ className }: { className?: string }) {
  return (
    <span
      className={cn(
        "inline-flex items-center gap-1 rounded-full bg-gold px-2.5 py-0.5 text-xs font-semibold text-gold-foreground shadow-sm",
        className,
      )}
    >
      <Star className="size-3.5 fill-current" aria-hidden />
      Destaque
    </span>
  );
}
