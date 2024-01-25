# Kyna

[Kyna](https://www.theparentz.com/baby-names/kyna) is an MIT-licensed, open-source project with the goal of creating a stable framework from which coders/traders/investors can collect historical stock data, backtest trading ideas against that data, and make informed decisions with their money.

What we're doing here is not groundbreaking science, but it is 100% transparent and grounded in a set of tools available to retail investors. Anyone is welcome to a copy of the code repository for their own purposes. Those who want to *contribute* are invited to do so - we could use the help and there are many challenging opportunities available within the scope of this project.

## Phase 1 (Current Phase)

### Goals

1. Construct a mechanism to collect and organize *historical* stock data for US markets.
    1. Equity data (required).
    1. Fundamental data (desired).
    1. Options data (desired).
1. Create a news collector.
    1. Map the news to our symbol (ticker) data.
    1. Determine sentiment when possible (NLP?). 
1. Construct first iteration of a backtesting system and use it to create accurate and reliable tables of technical analysis statistics for a variety of indicators (e.g. moving averages, candlestick patterns, etc.).

### Backtesting Approach

The backtesting approach will be as scientific as possible. Take a hypothesis (there are many to choose from), construct a test, run the test, and measure the results. Refine and repeat, abandon, or migrate the information into your trading strategy. 

The backtesting engine must never be allowed to look forward. Given any pattern or criterion, the backtesting engine must make all decisions based only on information available at the time of the event analyzed. This includes variables that are easier to attain using the whole data set, such as the average move per day or average size of a candlestick body. 

Many "indicators" or "patterns" evangelized in the finfluencer marketplace require that the market and/or price action be in a certain state in order for the indicator to be valid. An example of this is the "Bullish Engulfing" candlestick pattern; for this pattern to be "valid," it must occur in a "downtrend." For a human to look at a stock chart and determine trend, it's a relatively easy exercise; teaching the computer to determine trend is harder than it probably seems; for this reason, we're going to start the backtesting journey with determining trend on a stock chart.

Determining success of an indicator (e.g., does the stock go up after {INSERT PATTERN HERE}?) is not as easy as it might seem. Again, a trained human can look at a chart, easily see resistance and support, and make some quick risk/reward calculations. Asking the computer to do this is, again, trickier than it seems. For this reason, we intend to start with a simple up/down percentage ratio, asking a question such as: after {PATTERN}, is the stock more likely to go **up** or **down** 10%?

A baseline is required in order to properly judge an indicator. We will randomnly select thousands of locations on randomnly selected charts and check the up/down ratio for any pair of numbers we want to use as our measuring stick. In other words, by throwing a dart at the chart, is it more likely to go up or down 10% from that point? Any specific win/loss ratio (e.g., up/down 10% or up 20% / down 10%, etc.) will need an established baseline to determine efficacy. I know from experience that many indicators we test will perform worse than the random baseline, but more on that later.

Some potential baselines up/down ratios to start:

| Up  | Down | Notes             |
| --- | ---- | ----------------- |
|  5% |  5%  |                   |
| 10% | 10%  |                   |
| 20% | 20%  |                   |
| 24% |  8%  | Classic 3:1 ratio |

## Design Diagrams

### Context

![Kyna C4 Context](./docs/images/kyna-context.png)

### Architecture

![Kyna Architecture](./docs/images/kyna-architecture.png)

### Current Effort

![Phase 1.1 Plan](./docs/images//kyna-plans-phase-1-1.png)