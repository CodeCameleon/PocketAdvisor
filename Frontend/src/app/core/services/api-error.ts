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
   * Handles both flat keys ("CategoryId") and indexed array paths ("Items[0].ItemId").
   */
  private findControl(form: FormGroup, key: string): AbstractControl | null {
    const segments = this.parsePath(key);

    if (segments.length > 1) {
      let control: AbstractControl | null = form;

      for (const seg of segments) {
        if (!control) return null;

        if (typeof seg === 'number') {
          control = (control as import('@angular/forms').FormArray).at?.(seg) ?? null;
        } else {
          control = control.get(seg) ?? control.get(this.toCamelCase(seg));
        }
      }

      return control;
    }

    // Flat key — original behaviour.
    return form.get(key) ?? form.get(this.toCamelCase(key));
  }

  /**
   * Splits an ASP.NET Core error key into typed segments.
   * "Items[0].ItemId" → ["Items", 0, "ItemId"]
   */
  private parsePath(key: string): (string | number)[] {
    const segments: (string | number)[] = [];
    const re = /([^\.\[\]]+)|\[(\d+)\]/g;
    let match: RegExpExecArray | null;

    while ((match = re.exec(key)) !== null) {
      if (match[2] !== undefined) {
        segments.push(parseInt(match[2], 10));
      } else {
        segments.push(match[1]);
      }
    }

    return segments;
  }

  private toCamelCase(value: string): string {
    if (!value) return value;
    return value.charAt(0).toLowerCase() + value.slice(1);
  }
}
