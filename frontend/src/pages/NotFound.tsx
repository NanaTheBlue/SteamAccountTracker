import { Button } from '../components/ui/Button';

export function NotFound() {
  return (
    <div className="flex min-h-[70vh] flex-col items-center justify-center px-4 text-center">
      <h1
        className="glitch glitch-active inline-block text-[8rem] leading-none text-primary sm:text-[10rem]"
        data-text="404"
      >
        404
      </h1>
      <p className="mt-4 font-display text-2xl font-bold uppercase tracking-wider text-fg">
        You've been kicked from this route.
      </p>
      <p className="mb-10 mt-2 font-mono text-sm text-muted">Reason: it doesn't exist.</p>
      <Button to="/">Reconnect</Button>
    </div>
  );
}
