import { Injectable } from '@angular/core';
import { AbstractControl, FormGroup } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';

/**
 * Shape of ASP.NET Core ValidationProblemDetails returned on HTTP 400.
 * 'errors' is a map of PascalCase property name → array of messages.
 * The empty-string key "" holds global errors.
 */
interface ValidationProblemDetails {
  errors?: Record<string, string[]>;
}

/**
 * Parses server error responses and maps them onto reactive form controls.
 */
@Injectable({
  providedIn: 'root',
})
export class ApiErrorService {

  /**
   * Applies validation errors from a failed HTTP response to a FormGroup.
   *
   * - Field errors (keyed by property name) are set directly on the matching
   *   control via setErrors({ serverError: message }).
   * - Global errors are concatenated and returned as a plain string for the template to show.
   *
   * @param error   The HttpErrorResponse from the API call.
   * @param form    The FormGroup whose controls should receive field errors.
   * @returns       A string of global error messages, or '' if none.
   */
  applyErrors(error: HttpErrorResponse, form: FormGroup): string {
    const generalErrors: string[] = [];

    if (error.status === 409) {
      return 'A conflict occurred. This resource may already exist.';
    }

    const body = error.error as ValidationProblemDetails | null;
    const errorMap = body?.errors ?? {};

    for (const [key, messages] of Object.entries(errorMap)) {
      const message = messages[0] ?? '';

      if (!key) {
        generalErrors.push(message);
        continue;
      }

      const control = this.findControl(form, key);

      if (control) {
        control.setErrors({ serverError: message });
      } else {
        generalErrors.push(message);
      }
    }

    if (generalErrors.length === 0 && Object.keys(errorMap).length === 0) {
      return 'An unexpected error occurred. Please try again.';
    }

    return generalErrors.join(' ');
  }

  /**
   * Looks up a control by its server-side PascalCase property name.
   * Tries the key as-is first, then its camelCase equivalent.
   */
  private findControl(form: FormGroup, key: string): AbstractControl | null {
    return form.get(key) ?? form.get(this.toCamelCase(key));
  }

  private toCamelCase(value: string): string {
    if (!value) return value;
    return value.charAt(0).toLowerCase() + value.slice(1);
  }
}
