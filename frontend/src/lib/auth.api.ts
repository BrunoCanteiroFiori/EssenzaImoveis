import { apiFetch } from "./api-client";

export type UserRole = "SuperAdmin" | "Staff";
export type Permission = "ReadProperties" | "WriteProperties" | "DeleteProperties" | "ManageMessages";

export type AuthUser = {
  id: string;
  name: string;
  email: string;
  role: UserRole;
  permissions: Permission[];
};

export const login = (email: string, password: string) =>
  apiFetch<AuthUser>("/auth/login", { method: "POST", body: { email, password } });

export const logout = () => apiFetch<void>("/auth/logout", { method: "POST" });

export const getCurrentUser = () => apiFetch<AuthUser>("/auth/me");

export const changeOwnPassword = (currentPassword: string, newPassword: string) =>
  apiFetch<void>("/auth/change-password", { method: "POST", body: { currentPassword, newPassword } });
