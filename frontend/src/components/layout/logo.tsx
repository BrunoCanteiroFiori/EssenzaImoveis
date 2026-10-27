import Link from "next/link";
import { House } from "lucide-react";
import { siteConfig } from "@/lib/site-config";

export function Logo({ href = "/" }: { href?: string }) {
  return (
    <Link href={href} className="flex items-center gap-2 font-semibold tracking-tight">
      <span className="flex size-8 items-center justify-center rounded-lg bg-accent text-accent-foreground">
        <House className="size-4.5" aria-hidden />
      </span>
      <span className="text-base">{siteConfig.name}</span>
    </Link>
  );
}
