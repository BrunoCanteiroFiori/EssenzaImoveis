import { apiFetch } from "./api-client";
import type { Permission, UserRole } from "./auth.api";

export const permissionLabels: Record<Permission, string> = {
  ReadProperties: "Ver imóveis na gestão",
  WriteProperties: "Cadastrar e editar imóveis",
  DeleteProperties: "Excluir imóveis",
  ManageMessages: "Atender mensagens do site",
};

export const permissions = Object.keys(permissionLabels) as Permission[];

export type User = {
  id: string;
  name: string;
  email: string;
  role: UserRole;
  permissions: Permission[];
  isActive: boolean;
  lastSignInAt: string | null;
  createdAt: string;
  updatedAt: string;
};

export const listUsers = () => apiFetch<User[]>("/users");

export const createUser = (input: { name: string; email: string; password: string; permissions: Permission[] }) =>
  apiFetch<User>("/users", { method: "POST", body: input });

export const updateUser = (id: string, input: { name: string; email: string; permissions: Permission[] }) =>
  apiFetch<User>(`/users/${id}`, { method: "PUT", body: input });

export const activateUser = (id: string) => apiFetch<User>(`/users/${id}/activate`, { method: "POST" });

export const deactivateUser = (id: string) => apiFetch<User>(`/users/${id}/deactivate`, { method: "POST" });

export const resetUserPassword = (id: string, newPassword: string) =>
  apiFetch<void>(`/users/${id}/reset-password`, { method: "POST", body: { newPassword } });
