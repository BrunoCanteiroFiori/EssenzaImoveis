import { Suspense } from "react";
import type { Metadata } from "next";
import { Spinner } from "@/components/ui/feedback";
import { AuthComponent } from "./auth.component";

export const metadata: Metadata = {
  title: "Área restrita",
  robots: { index: false, follow: false },
};

export default function Page() {
  return (
    <div className="flex flex-1 items-center justify-center px-4 py-16">
      <Suspense fallback={<Spinner />}>
        <AuthComponent />
      </Suspense>
    </div>
  );
}
