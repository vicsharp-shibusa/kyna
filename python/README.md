# Python Scripts

## yahoo-import.py

Invokes a Yahoo Finance API SDK and writes data to CSV files in the specified output directory.

Data captured includes:

- historical price data
- split data
- dividend data
- financial data

To run the script:

```
py yahoo-import.py "./data" --prices --splits --dividends --financials
```