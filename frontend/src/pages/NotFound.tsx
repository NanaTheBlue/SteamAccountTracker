import * as React from 'react';
import { Link } from 'react-router';

export function NotFound() {
  return (
    <div className="min-h-[70vh] flex flex-col items-center justify-center px-4">
      <h1 className="text-6xl font-bold text-gray-700 mb-4">404</h1>
      <h2 className="text-2xl font-semibold text-gray-300 mb-8">Page not found</h2>
      <Link 
        to="/" 
        className="px-6 py-3 bg-blue-600 hover:bg-blue-700 text-white font-medium rounded-md transition-colors"
      >
        Return Home
      </Link>
    </div>
  );
}
