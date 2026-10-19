ALTER TABLE Users ADD
    PasswordResetTokenHash NVARCHAR(64) NULL,
    PasswordResetExp DATETIME2 NULL;

