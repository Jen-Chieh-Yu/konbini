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

export interface UserDTO {
  id: number
  name: string
  email: string
  phone?: string
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
