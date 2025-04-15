#!/bin/bash

# Find all .cs files, excluding obj directories
find . -name "*.cs" -not -path "*/obj/*" | while read -r file; do
  echo "Processing $file"
  
  # Remove single-line comments that don't start with ///
  sed -i '' -E '/^[[:space:]]*\/\/[^\/]/d' "$file"
  
  # Remove multi-line comments that don't start with /** or ///
  # This is more complex and would require a more sophisticated approach
  # For now, we'll focus on the single-line comments which are more common
done

echo "Comment removal complete"
