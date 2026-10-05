interface SpinnerProps {
  size?: number;
  label?: string;
  className?: string;
}

/** Rotating crosshair. */
export function Spinner({ size = 40, label = 'Loading', className = '' }: SpinnerProps) {
  return (
    <div role="status" className={`flex flex-col items-center gap-3 ${className}`}>
      <svg
        width={size}
        height={size}
        viewBox="0 0 40 40"
        fill="none"
        className="animate-spin-slow text-primary"
        aria-hidden="true"
      >
        <circle cx="20" cy="20" r="14" stroke="currentColor" strokeWidth="2" strokeDasharray="18 4" />
        <path d="M20 2v10M20 28v10M2 20h10M28 20h10" stroke="currentColor" strokeWidth="2" />
        <circle cx="20" cy="20" r="1.5" fill="currentColor" />
      </svg>
      <span className="mono-label cursor-blink">{label}</span>
    </div>
  );
}
