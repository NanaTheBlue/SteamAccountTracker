
ALTER TABLE SteamAccounts ADD
    VACBanned        BIT          NOT NULL DEFAULT 0,
    NumberOfVACBans  INT          NOT NULL DEFAULT 0,
    NumberOfGameBans INT          NOT NULL DEFAULT 0,
    CommunityBanned  BIT          NOT NULL DEFAULT 0,
    LastScannedAt    DATETIME2    NULL;

