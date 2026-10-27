import { apiFetch } from "./api-client";
import type { PropertySummary } from "./properties.types";

export type DashboardSummary = {
  availableProperties: number;
  reservedProperties: number;
  soldProperties: number;
  rentedProperties: number;
  inactiveProperties: number;
  sponsoredProperties: number;
  newMessages: number;
  recentViews: number;
  hottestProperties: PropertySummary[];
};

export const getDashboardSummary = () => apiFetch<DashboardSummary>("/dashboard");
