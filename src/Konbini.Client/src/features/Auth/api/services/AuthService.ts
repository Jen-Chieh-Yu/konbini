import instance from '@shared/api/axios'
import type { CaptchaAnswer } from '@features/Captcha/api/interfaces/CaptchaDTO'
import { CaptchaService } from '@features/Captcha/api/services/CaptchaService'

import { AuthApi } from '../constants/ApiEndpoints'
import type {
  ChangePasswordRequestDTO,
  LoginRequestDTO,
  LoginResponseDTO,
  RegisterRequestDTO,
  RequestPasswordResetRequestDTO,
  ResendVerificationEmailRequestDTO,
  ResetPasswordRequestDTO,
  UserDTO,
  VerifyEmailRequestDTO,
  VerifyEmailResponseDTO,
} from '../interfaces/AuthDTO'

export const AuthService = {
  /** 驗證碼沒過時回 400，errors 的 key 是 captcha */
  login: async (data: LoginRequestDTO, captcha: CaptchaAnswer): Promise<LoginResponseDTO> => {
    const res = await instance.post<LoginResponseDTO>(AuthApi.login, data, {
      headers: CaptchaService.toHeaders(captcha),
    })
    return res.data
  },

  register: async (data: RegisterRequestDTO) => {
    const res = await instance.post(AuthApi.register, data)
    return res.data
  },

  getMe: async (): Promise<UserDTO> => {
    const res = await instance.get<UserDTO>(AuthApi.me)
    return res.data
  },

  changePassword: async (data: ChangePasswordRequestDTO) => {
    const res = await instance.put(AuthApi.password, data)
    return res.data
  },

  verifyEmail: async (data: VerifyEmailRequestDTO): Promise<VerifyEmailResponseDTO> => {
    const res = await instance.post<VerifyEmailResponseDTO>(AuthApi.emailVerifications, data)
    return res.data
  },

  /** Email 不存在、已驗證或還在冷卻時間內也回成功（204），畫面不能說「已寄出」 */
  resendVerificationEmail: async (data: ResendVerificationEmailRequestDTO) => {
    await instance.post(AuthApi.verificationEmails, data)
  },

  /** Email 不存在或還在冷卻時間內也回成功（204），畫面不能說「已寄出」 */
  requestPasswordReset: async (data: RequestPasswordResetRequestDTO) => {
    await instance.post(AuthApi.passwordResetEmails, data)
  },

  resetPassword: async (data: ResetPasswordRequestDTO) => {
    await instance.post(AuthApi.passwordResets, data)
  },
}
