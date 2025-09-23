-- Manual migration for fixing SessionToken length constraint
-- Applied manually on 2025-09-23 to resolve JWT authentication issue
-- This documents the change for production deployment

-- Problem: JWT tokens exceed 500 character limit causing authentication failures
-- Solution: Change SessionToken column from VARCHAR(500) to TEXT

ALTER TABLE user_sessions ALTER COLUMN "SessionToken" TYPE TEXT;

-- Verify the change
-- Expected result: data_type should be 'text' with no character_maximum_length
SELECT
    column_name,
    data_type,
    character_maximum_length
FROM information_schema.columns
WHERE table_name = 'user_sessions'
AND column_name = 'SessionToken';