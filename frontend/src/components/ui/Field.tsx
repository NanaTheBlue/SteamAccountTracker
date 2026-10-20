import * as React from 'react';
import { useState } from 'react';

interface FieldProps extends React.InputHTMLAttributes<HTMLInputElement> {
  id: string;
  label: string;
  hint?: string;
  labelAction?: React.ReactNode;
  className?: string;
}

export function Field({ id, label, hint, labelAction, className = '', type, ...inputProps }: FieldProps) {
  const isPassword = type === 'password';
  const [showPassword, setShowPassword] = useState(false);
  const resolvedType = isPassword ? (showPassword ? 'text' : 'password') : type;

  return (
    <div className={className}>
      {labelAction ? (
        <div className="mb-1.5 flex items-baseline justify-between gap-2">
          <label htmlFor={id} className="field-label !mb-0">
            {label}
          </label>
          {labelAction}
        </div>
      ) : (
        <label htmlFor={id} className="field-label">
          {label}
        </label>
      )}
      <div className="relative">
        <input
          id={id}
          name={inputProps.name ?? id}
          type={resolvedType}
          className={`field-input ${isPassword ? 'pr-16' : ''}`}
          {...inputProps}
        />
        {isPassword && (
          <button
            type="button"
            onClick={() => setShowPassword(prev => !prev)}
            aria-label={showPassword ? 'Hide password' : 'Show password'}
            className="absolute inset-y-0 right-0 flex items-center px-3 font-mono text-[10px] uppercase tracking-wider text-muted hover:text-primary"
          >
            {showPassword ? 'Hide' : 'Show'}
          </button>
        )}
      </div>
      {hint && <p className="field-hint">{hint}</p>}
    </div>
  );
}
