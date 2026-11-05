import { texts } from '../../texts'

export interface RegisterErrors {
  email?: string
  password?: string
  form?: string
}

const messages: Record<string, string> = texts.auth.identityErrors
const emailCodes = ['DuplicateUserName', 'DuplicateEmail', 'InvalidEmail', 'InvalidUserName']

// Identity keys register errors by error code (e.g. PasswordTooShort, DuplicateUserName), not by field name.
// Custom texts override the server messages.
export function registerErrors(errors: Record<string, string[]>): RegisterErrors {
  const email = new Set<string>()
  const password = new Set<string>()
  const form = new Set<string>()

  for (const code of Object.keys(errors)) {
    if (code.startsWith('Password')) {
      password.add(messages[code] ?? texts.auth.passwordRejected)
    } else if (emailCodes.includes(code)) {
      email.add(messages[code])
    } else {
      form.add(texts.errors.unexpected)
    }
  }

  return { email: lines(email), password: lines(password), form: lines(form) }
}

// Each message appears on a separate line. The error paragraph preserves line breaks (white-space: pre-line).
function lines(messages: Set<string>) {
  return messages.size > 0 ? [...messages].join('\n') : undefined
}
