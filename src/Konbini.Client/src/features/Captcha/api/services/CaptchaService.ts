import instance from '@shared/api/axios'

import { CaptchaApi } from '../constants/ApiEndpoints'
import type { CaptchaAnswer, CaptchaDTO } from '../interfaces/CaptchaDTO'

export const CaptchaService = {
  create: async (): Promise<CaptchaDTO> => {
    const res = await instance.post<CaptchaDTO>(CaptchaApi.captchas)
    return res.data
  },

  /** 驗證碼放 header，不放 body：後端在 endpoint filter 檢查，各用例的 request 不用改 */
  toHeaders: (captcha: CaptchaAnswer): Record<string, string> => ({
    'X-Captcha-Id': captcha.id ?? '',
    'X-Captcha-Answer': captcha.answer,
  }),
}
