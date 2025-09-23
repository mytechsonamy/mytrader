#!/bin/bash
# Simple data import script for AGROT
echo "Starting AGROT data import test..."
curl -s "http://localhost:5245/api/dataimport/import-csv" \
  -X POST \
  -H "Content-Type: application/json" \
  -d '{"filePath":"/Users/mustafayildirim/Documents/Personal Documents/Projects/Stock_Scrapper/DATA/BIST/AGROT_historical.csv","dataSource":"YAHOO"}' \
  --max-time 60 \
  --connect-timeout 10
echo ""
echo "Import request sent."
