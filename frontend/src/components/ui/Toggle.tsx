interface ToggleProps {
  checked: boolean;
  onChange: () => void;
  label: string;
}

export function Toggle({ checked, onChange, label }: ToggleProps) {
  return (
    <button
      type="button"
      role="switch"
      aria-checked={checked}
      aria-label={label}
      onClick={onChange}
      className={`relative inline-flex h-7 w-16 flex-shrink-0 cursor-pointer items-center border font-mono text-[10px] font-bold tracking-widest transition-colors ${
        checked ? 'border-clean bg-clean/10 text-clean' : 'border-line-strong bg-canvas text-dim'
      }`}
    >
      <span className={`absolute w-full text-center transition-opacity ${checked ? 'pr-5' : 'pl-5'}`}>
        {checked ? 'ON' : 'OFF'}
      </span>
      <span
        className={`absolute top-1 h-[18px] w-[18px] transition-all duration-200 ${
          checked ? 'left-[calc(100%-22px)] bg-clean shadow-glow-clean' : 'left-1 bg-dim'
        }`}
      />
    </button>
  );
}
