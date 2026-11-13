import instance from '@shared/api/axios'

import { AuthApi } from '../constants/ApiEndpoints'
import type {
  ChangePasswordRequestDTO,
  LoginRequestDTO,
  LoginResponseDTO,
  RegisterRequestDTO,
  ResendVerificationEmailRequestDTO,
  UserDTO,
  VerifyEmailRequestDTO,
  VerifyEmailResponseDTO,
} from '../interfaces/AuthDTO'

export const AuthService = {
  login: async (data: LoginRequestDTO): Promise<LoginResponseDTO> => {
    const res = await instance.post<LoginResponseDTO>(AuthApi.login, data)
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
}
