#!/usr/bin/env python3

import pandas as pd
import psycopg2
import os
import glob
from datetime import datetime
import uuid

# Database connection
def get_db_connection():
    return psycopg2.connect(
        host="localhost",
        port=5434,
        database="mytrader",
        user="postgres",
        password="password"
    )

def process_csv_file(file_path, market_name):
    """Process a single CSV file and insert into market_data table"""
    print(f"Processing: {file_path}")

    try:
        # Read CSV file
        df = pd.read_csv(file_path)

        # Remove BOM if present
        if df.columns[0].startswith('\ufeff'):
            df.columns = [df.columns[0].replace('\ufeff', '')] + df.columns[1:].tolist()

        # Determine format and standardize column names
        if 'HisseKodu' in df.columns:
            # Standard format (use adjusted close if available)
            close_col = 'DuzeltilmisKapanis' if 'DuzeltilmisKapanis' in df.columns else 'KapanisFiyati'
            df = df.rename(columns={
                'HisseKodu': 'Symbol',
                'Tarih': 'Date',
                'AcilisFiyati': 'Open',
                'EnYuksek': 'High',
                'EnDusuk': 'Low',
                close_col: 'Close',
                'Hacim': 'Volume'
            })
        elif 'HGDG_HS_KODU' in df.columns:
            # BIST format (use adjusted close)
            df = df.rename(columns={
                'HGDG_HS_KODU': 'Symbol',
                'Tarih': 'Date',
                'AcilisFiyati': 'Open',
                'EnYuksek': 'High',
                'EnDusuk': 'Low',
                'DuzeltilmisKapanis': 'Close',  # Always use adjusted close for BIST
                'Hacim': 'Volume'
            })

        # Filter and clean data
        required_cols = ['Symbol', 'Date', 'Open', 'High', 'Low', 'Close', 'Volume']
        if not all(col in df.columns for col in required_cols):
            print(f"  ❌ Missing required columns in {file_path}")
            return 0

        df = df[required_cols].dropna()

        # Convert date format
        try:
            df['Date'] = pd.to_datetime(df['Date'])
        except:
            print(f"  ❌ Date parsing failed for {file_path}")
            return 0

        # Add required fields
        df['Id'] = [str(uuid.uuid4()) for _ in range(len(df))]
        df['Timeframe'] = 'DAILY'
        df['Timestamp'] = df['Date']
        df['AssetClass'] = market_name.upper()  # Add asset class based on market name

        # Connect to database and insert
        conn = get_db_connection()
        cur = conn.cursor()

        insert_count = 0
        for _, row in df.iterrows():
            try:
                # Scale down volume if too large for numeric(18,8)
                volume = float(row['Volume'])
                if volume > 99999999:  # Scale large volumes down
                    volume = volume / 1000000  # Convert to millions

                cur.execute("""
                    INSERT INTO market_data ("Id", "Symbol", "Timeframe", "Timestamp", "Open", "High", "Low", "Close", "Volume", "AssetClass")
                    VALUES (%s, %s, %s, %s, %s, %s, %s, %s, %s, %s)
                    ON CONFLICT ("Symbol", "Timeframe", "Timestamp") DO NOTHING
                """, (
                    row['Id'], row['Symbol'], row['Timeframe'], row['Timestamp'],
                    float(row['Open']), float(row['High']), float(row['Low']),
                    float(row['Close']), volume, row['AssetClass']
                ))
                conn.commit()  # Commit each row individually to avoid transaction block errors
                insert_count += 1
            except Exception as e:
                print(f"  ⚠️  Row insert failed: {e}")
                conn.rollback()  # Rollback this specific insert
                continue

        cur.close()
        conn.close()

        print(f"  ✅ Inserted {insert_count} records")
        return insert_count

    except Exception as e:
        print(f"  ❌ Error processing {file_path}: {e}")
        return 0

def main():
    print("🚀 MYTRADER HISTORICAL DATA IMPORT")
    print("==================================")

    base_path = "/Users/mustafayildirim/Documents/Personal Documents/Projects/Stock_Scrapper/DATA"
    markets = ["Crypto", "BIST", "NASDAQ", "NYSE"]

    total_files = 0
    total_records = 0

    for market in markets:
        market_path = os.path.join(base_path, market)
        if not os.path.exists(market_path):
            print(f"❌ Market directory not found: {market_path}")
            continue

        csv_files = glob.glob(os.path.join(market_path, "*.csv"))
        print(f"\n📈 Processing {market} market ({len(csv_files)} files)...")

        if market == "BIST":
            # Process only first 3 BIST files for testing
            for file_path in csv_files[:3]:
                records = process_csv_file(file_path, market)
                total_records += records
                total_files += 1
        else:
            # Skip already processed markets
            print(f"  ⏭️ {market} already processed, skipping...")
            continue

    print(f"\n🎉 IMPORT COMPLETE")
    print(f"Files processed: {total_files}")
    print(f"Records imported: {total_records}")

if __name__ == "__main__":
    main()