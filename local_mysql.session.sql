ALTER TABLE scg_records
ADD COLUMN IF NOT EXISTS scg_name VARCHAR(255) NOT NULL DEFAULT '' AFTER user_id;

UPDATE scg_records
SET scg_name = CONCAT(
    SUBSTRING_INDEX(SUBSTRING_INDEX(document_names_summary, ' / ', 1), '.', 1),
    '_SCG_V01'
)
WHERE scg_name = '' OR scg_name IS NULL;
