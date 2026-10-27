import { Flame, Snowflake, Thermometer, ThermometerSun } from "lucide-react";
import { cn } from "@/lib/cn";
import { temperatureLabels } from "@/lib/labels";
import type { Temperature, TemperatureLevel } from "@/lib/properties.types";

const styles: Record<TemperatureLevel, string> = {
  Cold: "bg-sky-100 text-sky-800 dark:bg-sky-950 dark:text-sky-300",
  Warm: "bg-amber-100 text-amber-800 dark:bg-amber-950 dark:text-amber-300",
  Hot: "bg-orange-100 text-orange-800 dark:bg-orange-950 dark:text-orange-300",
  VeryHot: "bg-red-100 text-red-700 dark:bg-red-950 dark:text-red-300",
};

const icons = { Cold: Snowflake, Warm: Thermometer, Hot: ThermometerSun, VeryHot: Flame } satisfies Record<TemperatureLevel, unknown>;

/** Temperatura do imóvel: quanto mais visitantes diferentes nos últimos 30 dias, mais quente. */
export function TemperatureBadge({ temperature, className }: { temperature: Temperature; className?: string }) {
  const Icon = icons[temperature.level];
  return (
    <span
      className={cn("inline-flex items-center gap-1 rounded-full px-2.5 py-0.5 text-xs font-medium", styles[temperature.level], className)}
      title={`${temperature.recentViews} visitas nos últimos 30 dias`}
    >
      <Icon className="size-3.5" aria-hidden />
      {temperatureLabels[temperature.level]}
    </span>
  );
}
