export interface LoginRequestDTO {
  email: string
  password: string
}

export interface RegisterRequestDTO {
  lastName: string
  firstName: string
  email: string
  password: string
  confirmPassword: string
  phoneNumber: string
  year: number
  month: number
  day: number
}

export interface ChangePasswordRequestDTO {
  currentPassword: string
  newPassword: string
}

/** 與後端 Auth/Models/UserRole.cs 對齊 */
export type UserRole = 'customer' | 'admin'

export interface UserDTO {
  id: number
  name: string
  email: string
  phone?: string
  /** 只用來決定要不要顯示後台入口；後台的防護在後端（不是管理員回 403） */
  role: UserRole
}

export interface LoginResponseDTO {
  token: string
  user: UserDTO
}

export interface VerifyEmailRequestDTO {
  token: string
}

export interface VerifyEmailResponseDTO {
  /** 這個連結之前就用過了（例：連點兩次），帳號早已完成驗證 */
  alreadyVerified: boolean
}

export interface ResendVerificationEmailRequestDTO {
  email: string
}

export interface RequestPasswordResetRequestDTO {
  email: string
}

export interface ResetPasswordRequestDTO {
  token: string
  newPassword: string
  confirmPassword: string
}
