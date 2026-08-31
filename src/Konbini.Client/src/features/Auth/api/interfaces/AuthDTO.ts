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
