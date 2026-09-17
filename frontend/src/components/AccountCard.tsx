import * as React from 'react';
import { useState } from 'react';

export interface TrackedAccountDto {
  steamId64: string;
  vacBanned: boolean;
  numberOfVACBans: number;
  numberOfGameBans: number;
  communityBanned: boolean;
}

interface AccountCardProps {
  account: TrackedAccountDto;
  onUntrack: (steamId64: string) => void;
}

export function AccountCard({ account, onUntrack }: AccountCardProps) {
  const [isConfirming, setIsConfirming] = useState(false);

  const totalBans = account.numberOfVACBans + account.numberOfGameBans;
  const isClean = !account.vacBanned && !account.communityBanned && totalBans === 0;

  return (
    <div className="bg-gray-800 rounded-lg p-5 border border-gray-700 flex flex-col h-full shadow-md hover:border-gray-600 transition-colors">
      <div className="flex justify-between items-start mb-4">
        <div>
          <h3 className="text-lg font-semibold text-gray-100 truncate" title={account.steamId64}>
            {account.steamId64}
          </h3>
          <a 
            href={`https://steamcommunity.com/profiles/${account.steamId64}`}
            target="_blank"
            rel="noopener noreferrer"
            className="text-sm text-blue-400 hover:text-blue-300 hover:underline"
          >
            View Profile ↗
          </a>
        </div>
        
        {isClean ? (
          <span className="px-2.5 py-1 text-xs font-semibold rounded-full bg-green-900/50 text-green-400 border border-green-800">
            Clean
          </span>
        ) : (
          <div className="flex flex-col gap-1 items-end">
            {account.vacBanned && (
              <span className="px-2.5 py-1 text-xs font-semibold rounded-full bg-red-900/50 text-red-400 border border-red-800">
                VAC Banned
              </span>
            )}
            {account.numberOfGameBans > 0 && (
              <span className="px-2.5 py-1 text-xs font-semibold rounded-full bg-red-900/50 text-red-400 border border-red-800">
                Game Banned
              </span>
            )}
            {account.communityBanned && (
              <span className="px-2.5 py-1 text-xs font-semibold rounded-full bg-red-900/50 text-red-400 border border-red-800">
                Community Banned
              </span>
            )}
          </div>
        )}
      </div>

      <div className="mt-2 mb-6 flex-grow">
        <div className="text-sm text-gray-400 space-y-1">
          <p>VAC Bans: <span className="text-gray-200 font-medium">{account.numberOfVACBans}</span></p>
          <p>Game Bans: <span className="text-gray-200 font-medium">{account.numberOfGameBans}</span></p>
        </div>
      </div>

      <div className="mt-auto">
        {isConfirming ? (
          <div className="flex gap-2">
            <button 
              onClick={() => onUntrack(account.steamId64)}
              className="flex-1 px-3 py-1.5 text-sm bg-red-600 hover:bg-red-700 text-white rounded transition-colors"
            >
              Confirm
            </button>
            <button 
              onClick={() => setIsConfirming(false)}
              className="flex-1 px-3 py-1.5 text-sm bg-gray-700 hover:bg-gray-600 text-white rounded transition-colors"
            >
              Cancel
            </button>
          </div>
        ) : (
          <button 
            onClick={() => setIsConfirming(true)}
            className="w-full px-3 py-1.5 text-sm border border-red-900/50 text-red-400 hover:bg-red-900/20 rounded transition-colors"
          >
            Untrack Account
          </button>
        )}
      </div>
    </div>
  );
}
