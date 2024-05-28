import createClient from "openapi-fetch";
import type { paths } from "./api.generated";

const baseUrl = import.meta.env.PUBLIC_URL;
export const backOfficeApi = createClient<paths>({ baseUrl });
