-- Remove subject_videos rows whose file_video path is not a valid stored MP4 reference.
DELETE FROM subject_videos
WHERE file_video IS NULL
   OR trim(file_video) = ''
   OR file_video NOT LIKE 'uploads/%'
   OR lower(file_video) NOT LIKE '%.mp4';
