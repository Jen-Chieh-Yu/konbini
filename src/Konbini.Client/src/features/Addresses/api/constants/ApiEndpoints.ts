export const AddressApi = {
  cities: '/api/addresses/cities',
  districts: (cityCode: number) => `/api/addresses/cities/${cityCode}/districts`,
} as const
