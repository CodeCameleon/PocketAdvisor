export interface ResetPasswordRequest {
  token: string;
  password: string;
  confirmPassword: string;
}
