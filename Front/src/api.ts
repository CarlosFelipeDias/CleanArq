import { csrfToken, expireSession } from './auth';
import axios from 'axios';
export interface Category { id: number; name: string }
export interface Product { id: number; name: string; description: string; price: number; stock: number; imageUrl: string; categoryId: number; category?: Category }
export type Resource = 'Products' | 'Categories';
const baseURL = import.meta.env.VITE_API_BASE_URL || '/api';
if (!baseURL.startsWith('/') || baseURL.startsWith('//')) throw new Error('The BFF API must use a same-origin relative URL.');
export const api = axios.create({ baseURL, timeout: 15000 });
api.interceptors.request.use(async config => {
  if (!['get', 'head', 'options'].includes(config.method?.toLowerCase() || 'get')) config.headers['X-CSRF-TOKEN'] = await csrfToken();
  return config;
});
api.interceptors.response.use(response => response, error => {
  if (error.response?.status === 401) { expireSession(); }
  return Promise.reject(error);
});
export async function list<T>(resource: Resource, signal?: AbortSignal): Promise<T[]> {
  try { return (await api.get<T[]>(`/${resource}`, { signal })).data; }
  catch (error) {
    // The existing categories endpoint reports an empty collection as 404.
    if (resource === 'Categories' && axios.isAxiosError(error) && error.response?.status === 404 && error.response.data === 'No categories found.') return [];
    throw error;
  }
}
export function errorMessage(error: unknown): string {
  if (!axios.isAxiosError(error)) return error instanceof Error ? error.message : 'Something went wrong. Please try again.';
  if (!error.response) return 'Unable to reach the API. Check that the backend is running and try again.';
  const data = error.response.data;
  if (Array.isArray(data)) return data.map(e => e.message || e.Message || 'Invalid value.').join(' ');
  if (data?.errors) return Object.values(data.errors).flat().join(' ');
  if (error.response.status === 401) return 'Your session has expired. Please sign in again.';
  if (error.response.status === 403) return 'You do not have permission to perform this action.';
  if (error.response.status === 500) return 'The server could not complete the request. Check that the API and database are available.';
  return typeof data === 'string' && data.length < 300 ? data : data?.message || data?.title || 'The request failed. Please try again.';
}
