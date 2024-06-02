import yfinance as yf
import pandas as pd
import argparse
import time
import os

def fetch_historical_data(ticker):
    try:
        data = yf.download(ticker) 
        return data
    except Exception as e:
        print(f"Error: could not fetch historical price data for {ticker}: {e}")

def fetch_split_data(ticker):
    try:
        stock = yf.Ticker(ticker)
        splits = stock.splits
        return splits
    except Exception as e:
        print(f"Error: Could not fetch split data for {ticker}: {e}")
        return None

def fetch_dividend_data(ticker):
    try:
        stock = yf.Ticker(ticker)
        dividends = stock.dividends
        return dividends
    except Exception as e:
        print(f"Error: Could not fetch dividend data for {ticker}: {e}")
        return pd.Series()

def fetch_financials(ticker):
    try:
        stock = yf.Ticker(ticker)
        financials = {
            "Financials": stock.financials,
            "Quarterly Financials": stock.quarterly_financials,
            "Balance Sheet": stock.balance_sheet,
            "Quarterly Balance Sheet": stock.quarterly_balance_sheet,
            "Cash Flow": stock.cashflow,
            "Quarterly Cash Flow": stock.quarterly_cashflow,
        }
        return financials
    except Exception as e:
        print(f"Error: Could not fetch financial data for {ticker}: {e}")
        return None
    
def write_financials_to_file(file_path, financials, ticker):
    try:
        for key, data in financials.items():
            if isinstance(data, pd.DataFrame):
                data.to_csv(f"{file_path}/{ticker}_{key.replace(' ', '_')}.csv")
        print(f"Financial data successfully written to files for {ticker}")
    except IOError as e:
        print(f"Error: Could not write to file for {ticker}: {e}")

def write_split_data_to_file(splits, file_name):
    try:
        splits.to_frame(name='Split Ratio').to_csv(file_name)
        print(f"Split data successfully written to {file_name}")
    except IOError as e:
        print(f"Error: Could not write to file {file_name}: {e}")

def write_dividend_data_to_file(dividends, file_name):
    try:
        dividends.to_frame(name='Dividends').to_csv(file_name)
        print(f"Dividend data successfully written to {file_name}")
    except IOError as e:
        print(f"Error: Could not write to file {file_name}: {e}")

def read_lines_from_file(file_path):
    try:
        with open(file_path, 'r') as file:
            lines = file.readlines()
        return [line.strip() for line in lines]
    except FileNotFoundError:
        print(f"Error: The file at {file_path} was not found.")
        return []
    except IOError as e:
        print(f"Error: An I/O error occurred: {e}")
        return []

def write_historical_data_to_file(file_path, data):
    try:
        data.to_csv(file_path)
        print(f"Data successfully written to {file_path}")
    except IOError as e:
        print(f"Error: Could not write to file {file_path}: {e}")

if __name__ == "__main__":
    start_time = time.time()
    parser = argparse.ArgumentParser(description='Fetch yahoo finance data.')
    parser.add_argument('output_path', type=str, help='Output path',default="./data")
    parser.add_argument('--prices', nargs='*', default=None)
    parser.add_argument('--splits', nargs='*', default=None)
    parser.add_argument('--dividends', nargs='*', default=None)
    parser.add_argument('--financials', nargs='*', default=None)
    args = parser.parse_args()

    include_prices = args.prices is not None
    include_splits = args.splits is not None
    include_dividends = args.dividends is not None
    include_financials = args.financials is not None

    file_count = 0;
    folder_index = 1
    target_path = f'{args.output_path}/{folder_index}'
    input_file_path = f'{args.output_path}/us_symbols.txt'
    if not os.path.exists(target_path):
        os.makedirs(target_path)

    lines = read_lines_from_file(input_file_path)

    if lines:
        # print(f"Read {len(lines)} lines from the file:")
        for symbol in lines:
            if symbol:
                if file_count > 1000:
                    file_count = 0
                    folder_index = folder_index + 1
                    target_path = f'{args.output_path}/{folder_index}'
                    if not os.path.exists(target_path):
                        os.makedirs(target_path)

                if include_prices:
                    historical_data = fetch_historical_data(symbol)
                    if not historical_data.empty:
                        file_count = file_count + 1
                        output_file_name = f"{target_path}/{symbol}_prices.csv"
                        write_historical_data_to_file(output_file_name, historical_data)
                
                if include_splits:
                    split_data = fetch_split_data(symbol)
                    if not split_data.empty:
                        file_count = file_count + 1
                        output_file_name = f"{target_path}/{symbol}_splits.csv"
                        write_split_data_to_file(split_data, output_file_name)
                
                if include_dividends:
                    dividend_data = fetch_dividend_data(symbol)
                    if not dividend_data.empty:
                        file_count = file_count + 1
                        output_file_name = f"{target_path}/{symbol}_dividends.csv"
                        write_dividend_data_to_file(dividend_data, output_file_name)

                if include_financials:
                    financials = fetch_financials(symbol)
                    if financials:
                        file_count = file_count + 6
                        write_financials_to_file(target_path, financials, symbol)
    
    end_time = time.time()
    elapsed_time = end_time - start_time
    
    if elapsed_time > 3600:
        print(f"Completed in {(elapsed_time / 3600):.2f} hours")
    elif elapsed_time > 60:
        print(f"Completed in {(elapsed_time / 60):.2f} minutes")
    else:
        print(f"Completed in {elapsed_time:.2f} seconds")