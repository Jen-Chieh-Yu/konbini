import instance from '@shared/api/axios'

import { AddressApi } from '../constants/ApiEndpoints'
import type { CityDTO, DistrictDTO } from '../interfaces/AddressDTO'

export const AddressService = {
  getCities: async (): Promise<CityDTO[]> => {
    const res = await instance.get<CityDTO[]>(AddressApi.cities)
    return res.data
  },

  getDistricts: async (cityCode: number): Promise<DistrictDTO[]> => {
    const res = await instance.get<DistrictDTO[]>(AddressApi.districts(cityCode))
    return res.data
  },
}
