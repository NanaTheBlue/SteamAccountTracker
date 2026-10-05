import * as React from 'react';

interface FieldProps extends React.InputHTMLAttributes<HTMLInputElement> {
  id: string;
  label: string;
  hint?: string;
  className?: string;
}

export function Field({ id, label, hint, className = '', ...inputProps }: FieldProps) {
  return (
    <div className={className}>
      <label htmlFor={id} className="field-label">
        {label}
      </label>
      <input id={id} name={inputProps.name ?? id} className="field-input" {...inputProps} />
      {hint && <p className="field-hint">{hint}</p>}
    </div>
  );
}
